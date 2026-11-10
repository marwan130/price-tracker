namespace PriceTracker.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PriceTracker.Application.DTOs.Common;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Infrastructure.Persistence;

[ApiController]
[Route("v1/discovery/overview")]
[AllowAnonymous]
[EnableRateLimiting("search")]
public sealed class DiscoveryOverviewController(ApplicationDbContext db, IPriceHistoryService history) : ControllerBase
{
    private const int WindowDays = 30;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since = now.AddDays(-WindowDays);
        var recentTrackings = db.UserProductTrackings.AsNoTracking()
            .Where(t => t.CreatedAt >= since && t.IsActive && t.User.IsActive && !t.User.IsTrackingPaused);

        var trending = await recentTrackings
            .GroupBy(t => new { t.ProductId, t.Product.Name, Category = t.Product.Category == null ? null : t.Product.Category.Name })
            .Select(group => new TrendingProductCard
            {
                ProductId = group.Key.ProductId,
                Name = group.Key.Name,
                Category = group.Key.Category,
                NewTrackingsInWindow = group.Count()
            })
            .OrderByDescending(item => item.NewTrackingsInWindow)
            .ThenBy(item => item.Name)
            .Take(8)
            .ToListAsync(ct);

        var popularCategories = await recentTrackings
            .Where(t => t.Product.CategoryId != null)
            .GroupBy(t => new { CategoryId = t.Product.CategoryId!.Value, Name = t.Product.Category!.Name })
            .Select(group => new PopularCategoryCard
            {
                CategoryId = group.Key.CategoryId,
                Name = group.Key.Name,
                NewTrackingsInWindow = group.Count()
            })
            .OrderByDescending(item => item.NewTrackingsInWindow)
            .ThenBy(item => item.Name)
            .Take(8)
            .ToListAsync(ct);

        var recentProducts = await db.Products.AsNoTracking()
            .Where(product => product.CreatedAt >= since && product.Listings.Any(listing => listing.IsActive
                && listing.PriceHistories.Any(point => point.Price > 0 && point.CurrencyCode.Length == 3)))
            .OrderByDescending(product => product.CreatedAt)
            .Take(8)
            .Select(product => new RecentProductCard
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Category = product.Category == null ? null : product.Category.Name,
                ImageUrl = product.Images.Where(image => image.IsPrimary).Select(image => image.Url).FirstOrDefault()
                    ?? product.Images.Select(image => image.Url).FirstOrDefault(),
                AddedAtUtc = product.CreatedAt,
                VerifiedOfferCount = product.Listings.Count(listing => listing.IsActive
                    && listing.PriceHistories.Any(point => point.Price > 0 && point.CurrencyCode.Length == 3))
            })
            .ToListAsync(ct);

        var recentStores = await db.Stores.AsNoTracking()
            .Where(store => store.IsActive && store.CreatedAt >= since && store.Listings.Any(listing => listing.IsActive
                && listing.PriceHistories.Any(point => point.Price > 0 && point.CurrencyCode.Length == 3)))
            .OrderByDescending(store => store.CreatedAt)
            .Take(8)
            .Select(store => new RecentStoreCard
            {
                StoreId = store.StoreId,
                Name = store.Name,
                Country = store.Country,
                CurrencyCode = store.CurrencyCode,
                AddedAtUtc = store.CreatedAt,
                VerifiedOfferCount = store.Listings.Count(listing => listing.IsActive
                    && listing.PriceHistories.Any(point => point.Price > 0 && point.CurrencyCode.Length == 3))
            })
            .ToListAsync(ct);

        var result = new DiscoveryOverviewResponse
        {
            GeneratedAtUtc = now,
            TrendingProducts = trending,
            PopularCategories = popularCategories,
            RecentlyAddedProducts = recentProducts,
            RecentlyAddedStores = recentStores,
            BestRecentDeals = await history.GetRecentDropsAsync(5)
        };
        return Ok(ApiResponse<DiscoveryOverviewResponse>.Ok(result));
    }
}
