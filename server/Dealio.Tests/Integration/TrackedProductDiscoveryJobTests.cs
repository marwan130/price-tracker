namespace Dealio.Tests.Integration;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Dealio.API.Infrastructure;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;

public sealed class TrackedProductDiscoveryJobTests : TestBase
{
    [Fact]
    public async Task ExecuteAsync_SchedulesOneDurableSessionPerTrackedProductMarketAndDeduplicates()
    {
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var currency = new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "E£" };
        var user = new User { Name = "Discovery", Email = "discovery-job@example.com", PasswordHash = "hash", IsActive = true };
        var product = new Product { Name = "Exact tracked item" };
        var store = new Store { Name = "Tracked store", BaseUrl = "https://tracked.example", Country = "EG", CurrencyCode = currency.Code };
        var listing = new StoreProductListing { Product = product, Store = store, ProductUrl = "https://tracked.example/item" };
        db.AddRange(currency, user, product, store, listing,
            new UserProductTracking { User = user, Product = product, Listing = listing, TargetPrice = 100, CurrencyCode = currency.Code });
        await db.SaveChangesAsync();

        var job = new TrackedProductDiscoveryJob(db, NullLogger<TrackedProductDiscoveryJob>.Instance);
        await job.ExecuteAsync();
        await job.ExecuteAsync();

        var session = await db.ProductSearchSessions.SingleAsync();
        session.SourceProductId.Should().Be(product.ProductId);
        session.MarketCode.Should().Be("EG");
        session.Status.Should().Be("queued");
        session.RequestPayload.Should().Contain("Exact tracked item").And.Contain("EG");
    }
}
