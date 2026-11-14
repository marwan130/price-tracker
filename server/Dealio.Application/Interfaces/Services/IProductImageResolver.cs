namespace Dealio.Application.Interfaces.Services;

/// <summary>
/// Resolves a product photo live from the retailer product page instead of
/// persisting images in the database: stored image URLs rot (CDNs expire,
/// formats rotate, hot-linking gets blocked), but the retailer link we track
/// the product with always carries its current primary photo.
/// </summary>
public interface IProductImageResolver
{
    /// <summary>Resolves the photo for a tracked product via its first active listing.</summary>
    Task<string?> ResolveForProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Resolves the photo directly from a retailer product page URL.</summary>
    Task<string?> ResolveForUrlAsync(Uri productUrl, CancellationToken ct = default);
}
