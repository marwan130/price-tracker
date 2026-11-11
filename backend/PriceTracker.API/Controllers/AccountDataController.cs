namespace PriceTracker.API.Controllers;

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceTracker.API.Extensions;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Infrastructure.Persistence;

[ApiController]
[Route("v1/users/me")]
[Authorize]
public sealed class AccountDataController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "json",
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? storeId = null,
        [FromQuery] string? market = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "fromUtc must be before or equal to toUtc."));
        if (!string.IsNullOrWhiteSpace(market) && PriceTracker.Domain.Markets.MiddleEastMarkets.Find(market) is null)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "market must be a supported market code."));

        var userId = User.GetUserId();
        var user = await db.Users.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => new { x.UserId, x.Name, x.Email, x.Phone, x.EmailVerified, x.IsTrackingPaused, x.CreatedAt })
            .SingleOrDefaultAsync(ct);
        if (user is null) return NotFound();

        var trackingQuery = db.UserProductTrackings.AsNoTracking()
            .Where(x => x.UserId == userId);
        if (productId.HasValue) trackingQuery = trackingQuery.Where(x => x.ProductId == productId.Value);
        if (storeId.HasValue) trackingQuery = trackingQuery.Where(x => x.Listing != null && x.Listing.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(market))
        {
            var country = PriceTracker.Domain.Markets.MiddleEastMarkets.Find(market)!.Country;
            trackingQuery = trackingQuery.Where(x => x.Listing != null && x.Listing.Store.Country == country);
        }
        if (fromUtc.HasValue) trackingQuery = trackingQuery.Where(x => x.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) trackingQuery = trackingQuery.Where(x => x.CreatedAt <= toUtc.Value);

        var trackings = await trackingQuery
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.TrackingId, x.ProductId, ProductName = x.Product.Name,
                x.ListingId, StoreName = x.Listing == null ? null : x.Listing.Store.Name,
                StoreUrl = x.Listing == null ? null : x.Listing.ProductUrl,
                x.TargetPrice, x.CurrencyCode, x.IsActive, x.NotifyEmail, x.NotifyFurtherDecreaseEmail,
                x.CreatedAt
            }).ToListAsync(ct);

        var productIds = trackings.Select(x => x.ProductId).Distinct().ToArray();
        var offerQuery = db.StoreProductListings.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId));
        if (storeId.HasValue) offerQuery = offerQuery.Where(x => x.StoreId == storeId.Value);
        if (!string.IsNullOrWhiteSpace(market))
        {
            var country = PriceTracker.Domain.Markets.MiddleEastMarkets.Find(market)!.Country;
            offerQuery = offerQuery.Where(x => x.Store.Country == country);
        }
        var offers = await offerQuery
            .OrderBy(x => x.ProductId).ThenBy(x => x.Store.Name)
            .Select(x => new { x.ListingId, x.ProductId, ProductName = x.Product.Name, StoreName = x.Store.Name, StoreCountry = x.Store.Country, x.ProductUrl, x.IsActive, x.LastScrapedAt })
            .ToListAsync(ct);
        var listingIds = offers.Select(x => x.ListingId).ToArray();
        var historyQuery = db.PriceHistories.AsNoTracking()
            .Where(x => listingIds.Contains(x.ListingId));
        if (fromUtc.HasValue) historyQuery = historyQuery.Where(x => x.RecordedAt >= fromUtc.Value);
        if (toUtc.HasValue) historyQuery = historyQuery.Where(x => x.RecordedAt <= toUtc.Value);
        var history = await historyQuery
            .OrderBy(x => x.RecordedAt)
            .Select(x => new { x.Id, x.ListingId, x.Price, x.CurrencyCode, x.RecordedAt, x.ScrapedAt })
            .ToListAsync(ct);
        var notificationQuery = db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId && trackings.Select(t => t.TrackingId).Contains(x.TrackingId));
        if (fromUtc.HasValue) notificationQuery = notificationQuery.Where(x => x.SentAt >= fromUtc.Value);
        if (toUtc.HasValue) notificationQuery = notificationQuery.Where(x => x.SentAt <= toUtc.Value);
        var notifications = await notificationQuery
            .OrderBy(x => x.SentAt)
            .Select(x => new { x.NotificationId, x.TrackingId, x.PriceHistoryId, x.TriggeredPrice, x.TargetPrice, x.EventType, x.Status, x.IsRead, x.SentAt })
            .ToListAsync(ct);

        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = new StringBuilder("trackingId,productId,productName,listingId,storeName,storeUrl,targetPrice,targetCurrency,isActive,notifyTargetEmail,notifyFurtherDecreaseEmail,createdAt\r\n");
            foreach (var item in trackings)
                csv.AppendJoin(',', new[] { item.TrackingId.ToString(), item.ProductId.ToString(), Csv(item.ProductName), item.ListingId?.ToString() ?? "", Csv(item.StoreName), Csv(item.StoreUrl), item.TargetPrice.ToString(System.Globalization.CultureInfo.InvariantCulture), item.CurrencyCode, item.IsActive.ToString(), item.NotifyEmail.ToString(), item.NotifyFurtherDecreaseEmail.ToString(), item.CreatedAt.ToString("O") }).Append("\r\n");
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "price-tracker-account.csv");
        }
        if (!format.Equals("json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "format must be json or csv."));

        return Ok(ApiResponse<object>.Ok(new
        {
            schemaVersion = 1,
            exportedAtUtc = DateTime.UtcNow,
            account = user,
            trackings,
            offers,
            priceHistory = history,
            notifications
        }));
    }

    private static string Csv(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.TrimStart() is { Length: > 0 } trimmed && trimmed[0] is '=' or '+' or '-' or '@') safe = $"'{safe}";
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
