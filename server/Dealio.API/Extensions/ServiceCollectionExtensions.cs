namespace Dealio.API.Extensions;

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using Dealio.Application;
using Dealio.API.Middleware;
using Dealio.Application.Interfaces.Repositories;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Services;
using Dealio.Application.Validators;
using Dealio.Infrastructure.Authentication;
using Dealio.Infrastructure.Email;
using Dealio.Infrastructure.Images;
using Dealio.Infrastructure.Persistence;
using Dealio.Infrastructure.Jobs;
using Dealio.Infrastructure.Persistence.Repositories;
using Dealio.Infrastructure.Caching;
using Dealio.Infrastructure.Currencies;
using Dealio.API.Infrastructure;
using Dealio.Infrastructure.Search;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration          config)
    {
        var rawConnection = config.GetConnectionString("Default");
        var parsedConnection = ParseConnectionUri(rawConnection);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(parsedConnection)
                   .ConfigureWarnings(warnings => 
                       warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        return services;
    }

    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration          config)
    {
        var connectionString = config.GetConnectionString("Redis");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            return services;
        }

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse(connectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry       = 3;
            configuration.ConnectTimeout     = 3000;
            return ConnectionMultiplexer.Connect(configuration);
        });

        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration          config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew                = TimeSpan.Zero,
                    ValidIssuer              = config["Jwt:Issuer"],
                    ValidAudience            = config["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!))
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = $"{ProductBrand.Name} API",
                Version     = "v1",
                Description = "REST API for tracking product prices across multiple stores"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "Bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter your JWT token"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }

    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration          config)
    {
        var rawConnection = config.GetConnectionString("Default");
        var parsedConnection = ParseConnectionUri(rawConnection);

        services.AddHangfire(cfg =>
            cfg.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(parsedConnection!)));

        services.AddHangfireServer();

        services.AddScoped<PriceAlertJob>();
        services.AddScoped<TrackedProductDiscoveryJob>();

        return services;
    }

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration          config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                      ?? (string.IsNullOrWhiteSpace(config["Cors:AllowedOrigins"]) ? [] : [config["Cors:AllowedOrigins"]!]);

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit          = 600,
                        TokensPerPeriod     = 600,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit          = 50,
                        AutoReplenishment   = true
                    }));

            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window      = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("search", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientPartitionKey(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit      = 90,
                        Window           = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit       = 10
                    }));

        });

        return services;
    }

    private static string GetClientPartitionKey(HttpContext context)
        => context.User.Identity?.Name
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ApiRequestMetrics>();
        services.AddScoped<IAuthService,           AuthService>();
        var deletionGraceDays = configuration.GetValue<int?>("Accounts:DeletionGraceDays") ?? 30;
        services.AddScoped<IUserService>(provider => new UserService(
            provider.GetRequiredService<IUserRepository>(), deletionGraceDays));
        services.AddScoped<ICategoryService,       CategoryService>();
        services.AddScoped<ICurrencyService,       CurrencyService>();
        services.AddScoped<IStoreService,          StoreService>();
        services.AddScoped<IProductService,        ProductService>();
        services.AddScoped<IListingService,        ListingService>();
        services.AddScoped<IPriceHistoryService,   PriceHistoryService>();
        services.AddScoped<ITrackingService,       TrackingService>();
        services.AddScoped<INotificationService,   NotificationService>();
        services.AddScoped<IScrapeLogService,      ScrapeLogService>();
        services.AddScoped<IPriceAlertService,     PriceAlertService>();
        services.AddScoped<IProductSearchService,  ProductSearchService>();
        services.AddHostedService<ProductSearchSessionWorker>();
        services.AddScoped<IProductCandidateVerificationQueue, OutboxProductCandidateVerificationQueue>();
        services.Configure<TavilyDiscoveryOptions>(configuration.GetSection("ProductDiscovery:Tavily"));
        services.AddHttpClient("product-discovery", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Dealio/1.0 (+product discovery)");
        });
        services.AddHttpClient("fx-rates", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Dealio/1.0 (+currency conversion)");
        });
        services.Configure<CurrencyRateOptions>(configuration.GetSection("CurrencyRates"));
        services.AddMemoryCache();
        services.AddSingleton<IProductDiscoveryProvider, TavilyProductDiscoveryProvider>();
        services.AddScoped<IProductDiscoveryProvider, LocalCatalogDiscoveryProvider>();
        services.AddScoped<ICurrencyConversionService, ExchangeRateConversionService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserRepository,           UserRepository>();
        services.AddScoped<ICategoryRepository,       CategoryRepository>();
        services.AddScoped<ICurrencyRepository,       CurrencyRepository>();
        services.AddScoped<IStoreRepository,          StoreRepository>();
        services.AddScoped<IProductRepository,        ProductRepository>();
        services.AddScoped<IListingRepository,        ListingRepository>();
        services.AddScoped<IPriceHistoryRepository,   PriceHistoryRepository>();
        services.AddScoped<ITrackingRepository,       TrackingRepository>();
        services.AddScoped<INotificationRepository,   NotificationRepository>();
        services.AddScoped<IScrapeLogRepository,      ScrapeLogRepository>();
        services.AddScoped<IListingScrapeQueue, OutboxListingScrapeQueue>();

        services.AddScoped<ISecureTokenService, SecureTokenService>();
        services.AddScoped<IGoogleIdentityTokenValidator, GoogleIdentityTokenValidator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher,  PasswordHasher>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();

        // Product photos are resolved live from the tracked retailer link (see
        // ProductImageResolver) rather than stored anywhere.
        services.AddScoped<IProductImageResolver, ProductImageResolver>();

        services.AddHttpClient<BrevoEmailSender>();
        services.AddScoped<IEmailSender>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return !string.IsNullOrWhiteSpace(configuration["Brevo:ApiKey"])
                ? provider.GetRequiredService<BrevoEmailSender>()
                : ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider);
        });

        services.AddHttpClient("product-search", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = ConnectToPublicAddressAsync
        });

        return services;
    }

    public static IServiceCollection AddRabbitMqWorkflow(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddHostedService<RabbitMqOutboxWorker>();
        services.AddHostedService<AccountDeletionWorker>();
        return services;
    }

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(
        SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        IPAddress[] addresses = IPAddress.TryParse(context.DnsEndPoint.Host, out var literal)
            ? new[] { literal }
            : await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        var publicAddress = addresses.FirstOrDefault(IsPublicAddress)
            ?? throw new HttpRequestException("The requested host does not resolve to a public IP address.");

        var socket = new Socket(publicAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(publicAddress, context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return !(bytes[0] == 0 || bytes[0] == 10 || bytes[0] == 127
                || bytes[0] == 169 && bytes[1] == 254
                || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
                || bytes[0] == 192 && bytes[1] == 168
                || bytes[0] >= 224);
        }

        var isGlobalUnicast = bytes[0] is >= 0x20 and <= 0x3f;
        var isDocumentationRange = bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8;
        return isGlobalUnicast && !isDocumentationRange
            && !(address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None)
            || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
            || (bytes[0] & 0xfe) == 0xfc);
    }

    public static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddTransient<DbHealthCheck>();
        services.AddTransient<RabbitMqHealthCheck>();
        services.AddHealthChecks()
                .AddCheck<DbHealthCheck>("db", tags: ["ready"])
                .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
        return services;
    }

    private static string? ParseConnectionUri(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return connectionString;

        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";

                var builder = new Npgsql.NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Username = username,
                    Password = password,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    SslMode = Npgsql.SslMode.Require
                };

                return builder.ToString();
            }
            catch
            {
                return connectionString;
            }
        }

        return connectionString;
    }
}

public class DbHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DbHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _context.Database.CanConnectAsync(cancellationToken))
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database is responding.");
            }
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}
