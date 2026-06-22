namespace PriceTracker.Tests.Integration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PriceTracker.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

public abstract class TestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    protected TestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("price_tracker_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    protected HttpClient Client => _client ?? throw new InvalidOperationException("Client not initialized");

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add the test database
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });

                    // Remove Hangfire for tests
                    var hangfireDescriptor = services.SingleOrDefault(
                        d => d.ServiceType.FullName?.Contains("Hangfire") == true);
                    if (hangfireDescriptor != null)
                    {
                        services.Remove(hangfireDescriptor);
                    }
                });
            });

        _client = _factory.CreateClient();

        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected async Task ResetDatabaseAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Delete all data
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
}
