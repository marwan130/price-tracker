namespace Dealio.Tests.Integration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Dealio.Infrastructure.Persistence;
using Dealio.Domain.Entities;
using Dealio.Domain.Enums;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public abstract class TestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly IContainer? _rabbitMqContainer;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    protected TestBase(bool enableRabbitMq = false)
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("dealio_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        if (enableRabbitMq)
        {
            _rabbitMqContainer = new ContainerBuilder()
                .WithImage("rabbitmq:4.1-alpine")
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "dealio_test")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "dealio_test_password")
                .WithPortBinding(5672, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
                .Build();
        }
    }

    protected HttpClient Client => _client ?? throw new InvalidOperationException("Client not initialized");
    protected IServiceScope CreateServiceScope() => _factory!.Services.CreateScope();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        if (_rabbitMqContainer is not null)
            await _rabbitMqContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"), optional: false);
                    if (_rabbitMqContainer is not null)
                    {
                        configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["RabbitMq:Enabled"] = "true",
                            ["RabbitMq:Host"] = "localhost",
                            ["RabbitMq:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                            ["RabbitMq:User"] = "dealio_test",
                            ["RabbitMq:Password"] = "dealio_test_password"
                        });
                    }
                });
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });
                    var hangfireDescriptor = services.SingleOrDefault(
                        d => d.ServiceType.FullName?.Contains("Hangfire") == true);
                    if (hangfireDescriptor != null)
                    {
                        services.Remove(hangfireDescriptor);
                    }
                });
            });

        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        if (_rabbitMqContainer is not null)
            await _rabbitMqContainer.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected async Task ResetDatabaseAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.PriceHistories.RemoveRange(context.PriceHistories);
        context.StoreProductListings.RemoveRange(context.StoreProductListings);
        context.Products.RemoveRange(context.Products);
        context.Stores.RemoveRange(context.Stores);
        context.Categories.RemoveRange(context.Categories);
        context.Currencies.RemoveRange(context.Currencies);
        context.Users.RemoveRange(context.Users);
        context.UserProductTrackings.RemoveRange(context.UserProductTrackings);
        context.Notifications.RemoveRange(context.Notifications);
        
        await context.SaveChangesAsync();
    }

    protected async Task SetUserRoleAsync(string email, UserRole role)
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.SingleAsync(x => x.Email == email);
        user.Role = role;
        await context.SaveChangesAsync();
    }

    protected async Task<Guid> SeedProductAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await context.Currencies.AnyAsync(x => x.Code == "USD"))
            context.Currencies.Add(new Currency { Code = "USD", Name = "US Dollar", Symbol = "$" });
        var product = new Product { Name = "Test Product", Description = "Test Description" };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.ProductId;
    }
}
