namespace PriceTracker.Application.DTOs.Products;

using PriceTracker.Application.DTOs.PriceHistory;

public sealed class DiscoveryOverviewResponse
{
    public DateTime GeneratedAtUtc { get; set; }
    public int ActivityWindowDays { get; set; } = 30;
    public IReadOnlyList<TrendingProductCard> TrendingProducts { get; set; } = [];
    public IReadOnlyList<PopularCategoryCard> PopularCategories { get; set; } = [];
    public IReadOnlyList<RecentProductCard> RecentlyAddedProducts { get; set; } = [];
    public IReadOnlyList<RecentStoreCard> RecentlyAddedStores { get; set; } = [];
    public IReadOnlyList<RecentPriceDropResponse> BestRecentDeals { get; set; } = [];
}

public sealed class TrendingProductCard
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public int NewTrackingsInWindow { get; set; }
}

public sealed class PopularCategoryCard
{
    public long CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NewTrackingsInWindow { get; set; }
}

public sealed class RecentProductCard
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime AddedAtUtc { get; set; }
    public int VerifiedOfferCount { get; set; }
}

public sealed class RecentStoreCard
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? CurrencyCode { get; set; }
    public DateTime AddedAtUtc { get; set; }
    public int VerifiedOfferCount { get; set; }
}
