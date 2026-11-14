namespace Dealio.API.Infrastructure;

using Hangfire;
using Microsoft.EntityFrameworkCore;
using Dealio.Application.Interfaces.Services;
using Dealio.Infrastructure.Persistence;

public sealed class ActiveOfferRefreshJob(
    ApplicationDbContext db,
    IListingScrapeQueue scrapeQueue,
    ILogger<ActiveOfferRefreshJob> logger)
{
    private static readonly TimeSpan ObservationAge = TimeSpan.FromHours(6);
    private static readonly TimeSpan QueueCooldown = TimeSpan.FromHours(4);
    private const int BatchSize = 200;

    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;
        var staleBefore = now.Subtract(ObservationAge);
        var queueBefore = now.Subtract(QueueCooldown);
        var listings = await db.StoreProductListings
            .Where(listing => listing.IsActive && !listing.IsExcluded && listing.Store.IsActive
                && (listing.Store.AutoSuspendedUntilUtc == null || listing.Store.AutoSuspendedUntilUtc <= now)
                && listing.Trackings.Any(tracking => tracking.IsActive
                    && tracking.User.IsActive && !tracking.User.IsTrackingPaused)
                && (listing.LastScrapedAt == null || listing.LastScrapedAt < staleBefore)
                && (listing.LastRefreshQueuedAtUtc == null || listing.LastRefreshQueuedAtUtc < queueBefore))
            .OrderBy(listing => listing.LastScrapedAt)
            .Take(BatchSize)
            .ToListAsync();

        foreach (var listing in listings)
        {
            scrapeQueue.Stage(listing.ListingId, listing.ProductUrl);
            listing.LastRefreshQueuedAtUtc = now;
        }

        if (listings.Count > 0)
            await db.SaveChangesAsync();

        logger.LogDebug("Queued {Count} stale offers for refresh", listings.Count);
    }
}
