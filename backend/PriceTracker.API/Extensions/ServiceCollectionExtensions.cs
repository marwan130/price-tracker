namespace PriceTracker.API.Extensions;

using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PriceTracker.API.Middleware;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Application.Services;
using PriceTracker.Application.Validators;
using PriceTracker.Infrastructure.Authentication;
using PriceTracker.Infrastructure.Email;
using PriceTracker.Infrastructure.Persistence;
using PriceTracker.Infrastructure.Jobs;
using PriceTracker.Infrastructure.Persistence.Repositories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration          config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Default")));

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
                    ValidIssuer              = config["Jwt:Issuer"],
                    ValidAudience            = config["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!))
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Smart Price Tracker API",
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
        services.AddHangfire(cfg =>
            cfg.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(config.GetConnectionString("Default")!)));

        services.AddHangfireServer();

        services.AddScoped<PriceAlertJob>();

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
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

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

            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window      = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService,           AuthService>();
        services.AddScoped<IUserService,           UserService>();
        services.AddScoped<ICategoryService,       CategoryService>();
        services.AddScoped<ICurrencyService,       CurrencyService>();
        services.AddScoped<IStoreService,          StoreService>();
        services.AddScoped<IProductService,        ProductService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IListingService,        ListingService>();
        services.AddScoped<IPriceHistoryService,   PriceHistoryService>();
        services.AddScoped<ITrackingService,       TrackingService>();
        services.AddScoped<INotificationService,   NotificationService>();
        services.AddScoped<IScrapeLogService,      ScrapeLogService>();
        services.AddScoped<IPriceAlertService,     PriceAlertService>();
        services.AddScoped<IAttributeTypeService,  AttributeTypeService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository,           UserRepository>();
        services.AddScoped<ICategoryRepository,       CategoryRepository>();
        services.AddScoped<ICurrencyRepository,       CurrencyRepository>();
        services.AddScoped<IStoreRepository,          StoreRepository>();
        services.AddScoped<IProductRepository,        ProductRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IListingRepository,        ListingRepository>();
        services.AddScoped<IPriceHistoryRepository,   PriceHistoryRepository>();
        services.AddScoped<ITrackingRepository,       TrackingRepository>();
        services.AddScoped<INotificationRepository,   NotificationRepository>();
        services.AddScoped<IScrapeLogRepository,      ScrapeLogRepository>();
        services.AddScoped<IAttributeTypeRepository,  AttributeTypeRepository>();

        // Authentication
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher,  PasswordHasher>();
        services.AddScoped<RefreshTokenStore>();

        // Email
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}