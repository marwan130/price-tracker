namespace Dealio.Infrastructure.Images;

using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Dealio.Application.Interfaces.Services;
using Dealio.Application.Scrapers;
using Dealio.Infrastructure.Persistence;

public sealed class ProductImageResolver(
    IHttpClientFactory httpClientFactory,
    ApplicationDbContext db,
    IMemoryCache localCache,
    ICacheService? sharedCache = null,
    ILogger<ProductImageResolver>? logger = null) : IProductImageResolver
{
    private const int HeadByteBudget = 384 * 1024;
    private static readonly TimeSpan PositiveCacheLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan NegativeCacheLifetime = TimeSpan.FromHours(2);
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(12);

    public async Task<string?> ResolveForProductAsync(Guid productId, CancellationToken ct = default)
    {
        var cacheKey = $"img-resolve:p:{productId:N}";
        var cached = await GetCachedUrlAsync(cacheKey, ct);
        if (cached is not null) return cached.Length == 0 ? null : cached;

        var listingUrl = await db.StoreProductListings.AsNoTracking()
            .Where(listing => listing.ProductId == productId && listing.IsActive)
            .OrderByDescending(listing => listing.LastScrapedAt)
            .Select(listing => listing.ProductUrl)
            .FirstOrDefaultAsync(ct);

        string? resolved = null;
        if (!string.IsNullOrWhiteSpace(listingUrl)
            && Uri.TryCreate(listingUrl, UriKind.Absolute, out var pageUrl)
            && pageUrl.Scheme == Uri.UriSchemeHttps
            && ScraperHelpers.IsProductDetailUrl(listingUrl))
            resolved = await FetchAndMineAsync(pageUrl);

        if (resolved is not null)
        {
            localCache.Set(cacheKey, resolved, PositiveCacheLifetime);
            if (sharedCache is not null) await sharedCache.SetAsync(cacheKey, resolved, PositiveCacheLifetime, ct);
            return resolved;
        }

        localCache.Set(cacheKey, string.Empty, NegativeCacheLifetime);
        if (sharedCache is not null) await sharedCache.SetAsync(cacheKey, string.Empty, NegativeCacheLifetime, ct);
        return null;
    }

    public async Task<string?> ResolveForUrlAsync(Uri pageUrl, CancellationToken ct = default)
    {
        if (pageUrl.Scheme != Uri.UriSchemeHttps || !ScraperHelpers.IsProductDetailUrl(pageUrl.ToString()))
            return null;

        var cacheKey = $"img-resolve:u:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pageUrl.ToString()))).ToLowerInvariant()}";
        var cached = await GetCachedUrlAsync(cacheKey, ct);
        if (cached is not null) return cached.Length == 0 ? null : cached;

        var resolved = await FetchAndMineAsync(pageUrl);
        if (resolved is not null)
        {
            localCache.Set(cacheKey, resolved, PositiveCacheLifetime);
            if (sharedCache is not null) await sharedCache.SetAsync(cacheKey, resolved, PositiveCacheLifetime, ct);
            return resolved;
        }

        localCache.Set(cacheKey, string.Empty, NegativeCacheLifetime);
        if (sharedCache is not null) await sharedCache.SetAsync(cacheKey, string.Empty, NegativeCacheLifetime, ct);
        return null;
    }

    private async Task<string?> GetCachedUrlAsync(string cacheKey, CancellationToken ct)
    {
        if (localCache.TryGetValue(cacheKey, out string? local)) return local;
        if (sharedCache is null) return null;
        try
        {
            return await sharedCache.GetAsync<string>(cacheKey, ct);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Image-resolve shared cache read failed; falling back to a live fetch.");
            return null;
        }
    }

    private async Task<string?> FetchAndMineAsync(Uri pageUrl)
    {
        var client = httpClientFactory.CreateClient("product-search");
        string? image = null;
        Exception? failure = null;
        for (var attempt = 0; attempt < 2 && image is null; attempt++)
        {
            if (attempt > 0) await Task.Delay(1500); 
            try
            {
                using var response = await SendFollowingRedirectsAsync(client, pageUrl);
                if (response is null)
                {
                    logger?.LogDebug("Image resolve stopped on a non-https redirect for {Url}.", pageUrl);
                    continue;
                }
                if (!response.IsSuccessStatusCode)
                {
                    logger?.LogDebug("Image resolve fetch for {Url} returned {Status}.", pageUrl, (int)response.StatusCode);
                    continue;
                }
                var head = await ReadHeadAsync(response.Content);
                image = ProductPageImageMiner.Mine(head, response.RequestMessage?.RequestUri ?? pageUrl);
                if (image is null)
                    logger?.LogDebug("Image resolve mined no photo from {Length} bytes of {Url}.", head.Length, pageUrl);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
            {
                failure = ex;
            }
        }
        if (image is null && failure is not null)
            logger?.LogDebug(failure, "Image resolve fetch failed for {Url}.", pageUrl);
        return image;
    }
    
    private static async Task<HttpResponseMessage?> SendFollowingRedirectsAsync(HttpClient client, Uri pageUrl)
    {
        using var timeout = new CancellationTokenSource(FetchTimeout);
        var current = pageUrl;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.TryAddWithoutValidation("User-Agent", ScraperHelpers.RandomUserAgent());
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Referer", current.GetLeftPart(UriPartial.Authority));
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!IsRedirect(response.StatusCode) || response.Headers.Location is not { } location)
                return response;
            response.Dispose();

            var next = location.IsAbsoluteUri ? location : new Uri(current, location);
            if (next.Scheme != Uri.UriSchemeHttps) return null;
            current = new UriBuilder(next) { Scheme = Uri.UriSchemeHttps, Port = 443 }.Uri;
        }
        return null;
    }

    private static bool IsRedirect(HttpStatusCode status)
        => status is HttpStatusCode.MultipleChoices or HttpStatusCode.MovedPermanently or HttpStatusCode.Found
            or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static async Task<string> ReadHeadAsync(HttpContent content)
    {
        await using var stream = await content.ReadAsStreamAsync();
        var buffer = new byte[HeadByteBudget];
        var read = 0;
        while (read < HeadByteBudget)
        {
            var chunk = await stream.ReadAsync(buffer.AsMemory(read, HeadByteBudget - read));
            if (chunk == 0) break;
            read += chunk;
        }
        return Encoding.UTF8.GetString(buffer, 0, read);
    }
}
