namespace Dealio.API.Infrastructure;

using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Dealio.Application.DTOs.Products;
using Dealio.Domain.Entities;
using Dealio.Domain.Markets;
using Dealio.Infrastructure.Persistence;

public sealed class TrackedProductDiscoveryJob(ApplicationDbContext db, ILogger<TrackedProductDiscoveryJob> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int BatchSize = 100;

    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-20);
        var targets = await db.UserProductTrackings.AsNoTracking()
            .Where(t => t.IsActive && t.User.IsActive && !t.User.IsTrackingPaused
                && t.Listing != null && t.Listing.IsActive && !t.Listing.IsExcluded)
            .Select(t => new
            {
                t.ProductId,
                ProductName = t.Product.Name,
                Country = t.Listing!.Store.Country
            })
            .Distinct()
            .ToListAsync(cancellationToken);
        var supportedTargets = targets
            .Select(target => new { target.ProductId, target.ProductName, Market = MiddleEastMarkets.FindByCountry(target.Country)?.Code })
            .Where(target => target.Market is not null)
            .Distinct()
            .OrderBy(target => target.ProductId)
            .Take(BatchSize)
            .ToList();

        var scheduled = 0;
        foreach (var target in supportedTargets)
        {
            var alreadyQueued = await db.ProductSearchSessions.AnyAsync(s =>
                s.SourceProductId == target.ProductId && s.MarketCode == target.Market
                && s.CreatedAtUtc >= cutoff && s.Status != "failed", cancellationToken);
            if (alreadyQueued) continue;

            var request = new ProductFilterRequest { Query = target.ProductName, Market = target.Market };
            db.ProductSearchSessions.Add(new ProductSearchSession
            {
                SessionId = Guid.NewGuid(),
                SourceProductId = target.ProductId,
                MarketCode = target.Market,
                RequestPayload = JsonSerializer.Serialize(request, JsonOptions),
                Status = "queued",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(1)
            });
            scheduled++;
        }

        if (scheduled > 0) await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Scheduled {Count} product-market rediscovery searches", scheduled);
    }
}
