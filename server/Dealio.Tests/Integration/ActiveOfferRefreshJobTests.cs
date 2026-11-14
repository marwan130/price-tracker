namespace Dealio.Tests.Integration;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Dealio.API.Infrastructure;
using Dealio.Application.Interfaces.Services;
using Dealio.Domain.Entities;
using Dealio.Infrastructure.Persistence;

public sealed class ActiveOfferRefreshJobTests : TestBase
{
    [Fact]
    public async Task ExecuteAsync_SkipsAutomaticallySuspendedStore_AndRetriesAfterCooldown()
    {
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var currency = new Currency { Code = "EGP", Name = "Egyptian Pound", Symbol = "ج.م" };
        var user = new User { Name = "Active", Email = "suspended-refresh@example.com", PasswordHash = "hash", IsActive = true };
        var product = new Product { Name = "Suspension test product" };
        var store = new Store
        {
            Name = "Suspended Store", BaseUrl = "https://suspended.example", Country = "EG",
            CurrencyCode = currency.Code, ConsecutiveScrapeFailures = StoreScrapeHealthPolicy.FailureThreshold,
            AutoSuspendedUntilUtc = DateTime.UtcNow.AddHours(1)
        };
        var listing = new StoreProductListing
        {
            Product = product, Store = store,
            ProductUrl = "https://suspended.example/item", LastScrapedAt = DateTime.UtcNow.AddHours(-8)
        };
        db.AddRange(currency, user, product, store, listing,
            new UserProductTracking { User = user, Product = product, Listing = listing, TargetPrice = 100, CurrencyCode = currency.Code });
        await db.SaveChangesAsync();

        var queued = new List<Guid>();
        var queue = new Mock<IListingScrapeQueue>();
        queue.Setup(x => x.Stage(It.IsAny<Guid>(), It.IsAny<string>()))
            .Callback<Guid, string>((listingId, _) => queued.Add(listingId));
        var job = new ActiveOfferRefreshJob(db, queue.Object, NullLogger<ActiveOfferRefreshJob>.Instance);

        await job.ExecuteAsync();
        queued.Should().BeEmpty();

        store.AutoSuspendedUntilUtc = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        await job.ExecuteAsync();

        queued.Should().ContainSingle().Which.Should().Be(listing.ListingId);
    }

    [Fact]
    public async Task ExecuteAsync_QueuesOnlyStaleOffersForActiveUnpausedTrackings_AndHonorsCooldown()
    {
        using var scope = CreateServiceScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var currency = new Currency { Code = "USD", Name = "US Dollar", Symbol = "$" };
        var activeUser = new User { Name = "Active", Email = "active-refresh@example.com", PasswordHash = "hash", IsActive = true };
        var pausedUser = new User { Name = "Paused", Email = "paused-refresh@example.com", PasswordHash = "hash", IsActive = true, IsTrackingPaused = true };
        var product = new Product { Name = "Refresh test product" };
        var storeOne = new Store { Name = "Store One", BaseUrl = "https://one.example", Country = "EG", CurrencyCode = currency.Code };
        var storeTwo = new Store { Name = "Store Two", BaseUrl = "https://two.example", Country = "EG", CurrencyCode = currency.Code };
        var storeThree = new Store { Name = "Store Three", BaseUrl = "https://three.example", Country = "EG", CurrencyCode = currency.Code };
        var activeListing = new StoreProductListing
        {
            Product = product, Store = storeOne,
            ProductUrl = "https://one.example/item", LastScrapedAt = DateTime.UtcNow.AddHours(-8)
        };
        var excludedListing = new StoreProductListing
        {
            Product = product, Store = storeTwo,
            ProductUrl = "https://two.example/item", IsExcluded = true, LastScrapedAt = DateTime.UtcNow.AddHours(-8)
        };
        var pausedListing = new StoreProductListing
        {
            Product = product, Store = storeThree,
            ProductUrl = "https://three.example/item", LastScrapedAt = DateTime.UtcNow.AddHours(-8)
        };

        db.AddRange(currency, activeUser, pausedUser, product,
            storeOne, storeTwo, storeThree, activeListing, excludedListing, pausedListing,
            new UserProductTracking { User = activeUser, Product = product, Listing = activeListing, TargetPrice = 100, CurrencyCode = currency.Code },
            new UserProductTracking { User = activeUser, Product = product, Listing = excludedListing, TargetPrice = 100, CurrencyCode = currency.Code },
            new UserProductTracking { User = pausedUser, Product = product, Listing = pausedListing, TargetPrice = 100, CurrencyCode = currency.Code });
        await db.SaveChangesAsync();

        var queued = new List<Guid>();
        var queue = new Mock<IListingScrapeQueue>();
        queue.Setup(x => x.Stage(It.IsAny<Guid>(), It.IsAny<string>()))
            .Callback<Guid, string>((listingId, _) => queued.Add(listingId));
        var job = new ActiveOfferRefreshJob(db, queue.Object, NullLogger<ActiveOfferRefreshJob>.Instance);

        await job.ExecuteAsync();
        await job.ExecuteAsync();

        queued.Should().Equal(activeListing.ListingId);
        (await db.OutboxMessages.CountAsync()).Should().Be(0, "the test queue captures work without writing outbox rows");
        var refreshed = await db.StoreProductListings.SingleAsync(x => x.ListingId == activeListing.ListingId);
        refreshed.LastRefreshQueuedAtUtc.Should().NotBeNull();
    }
}
