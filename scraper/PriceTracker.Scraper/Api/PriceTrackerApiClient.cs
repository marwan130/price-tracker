namespace PriceTracker.Scraper.Api;

using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceTracker.Scraper.Configuration;

public class PriceTrackerApiClient : IPriceTrackerApiClient
{
    private readonly HttpClient        _http;
    private readonly ILogger<PriceTrackerApiClient> _logger;
    private readonly int _listingPageSize;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PriceTrackerApiClient(
        HttpClient                    http,
        IOptions<ApiOptions>          apiOptions,
        IOptions<ScraperOptions>      scraperOptions,
        ILogger<PriceTrackerApiClient> logger)
    {
        _http   = http;
        _logger = logger;

        var api = apiOptions.Value;
        _listingPageSize = Math.Clamp(scraperOptions.Value.ListingPageSize, 1, 500);
        _http.BaseAddress = new Uri(api.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add("X-Internal-Key", api.InternalKey);
    }

    public async Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsAsync(CancellationToken ct = default)
    {
        var listings = new List<ScrapeListingDto>();

        for (var page = 0; ; page++)
        {
            var pageListings = await GetActiveListingsPageAsync(page, ct);
            listings.AddRange(pageListings);

            if (pageListings.Count < _listingPageSize)
                return listings;
        }
    }

    public async Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsAsync(string? query = null, int? categoryId = null, Guid? storeId = null, decimal? minPrice = null, decimal? maxPrice = null, string? currencyCode = null, CancellationToken ct = default)
    {
        var listings = new List<ScrapeListingDto>();

        for (var page = 0; ; page++)
        {
            var pageListings = await GetActiveListingsPageAsync(page, query, categoryId, storeId, minPrice, maxPrice, currencyCode, ct);
            listings.AddRange(pageListings);

            if (pageListings.Count < _listingPageSize)
                return listings;
        }
    }

    private async Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsPageAsync(int page, CancellationToken ct)
    {
        var response = await _http.GetAsync($"v1/internal/listings/active?page={page}&size={_listingPageSize}", ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to fetch active listings ({StatusCode}): {Body}",
                (int)response.StatusCode,
                body);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<ScrapeListingDto>>>(JsonOptions, ct);
        return payload?.Data ?? [];
    }

    private async Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsPageAsync(int page, string? query, int? categoryId, Guid? storeId, decimal? minPrice, decimal? maxPrice, string? currencyCode, CancellationToken ct)
    {
        var queryParams = new List<string> { $"page={page}", $"size={_listingPageSize}" };
        
        if (!string.IsNullOrWhiteSpace(query)) queryParams.Add($"query={Uri.EscapeDataString(query)}");
        if (categoryId.HasValue) queryParams.Add($"categoryId={categoryId.Value}");
        if (storeId.HasValue) queryParams.Add($"storeId={storeId.Value}");
        if (minPrice.HasValue) queryParams.Add($"minPrice={minPrice.Value}");
        if (maxPrice.HasValue) queryParams.Add($"maxPrice={maxPrice.Value}");
        if (!string.IsNullOrEmpty(currencyCode)) queryParams.Add($"currencyCode={Uri.EscapeDataString(currencyCode)}");
        
        var url = $"v1/internal/listings/active?{string.Join("&", queryParams)}";
        var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to fetch active listings with filters ({StatusCode}): {Body}",
                (int)response.StatusCode,
                body);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<ScrapeListingDto>>>(JsonOptions, ct);
        return payload?.Data ?? [];
    }

    public async Task PostPriceRecordAsync(CreatePriceRecordDto record, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("v1/price-history", record, ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            response = await RetryAfterAsync(response, () => _http.PostAsJsonAsync("v1/price-history", record, ct), ct);
        await EnsureSuccessAsync(response, "post price record", ct);
    }

    public async Task PostScrapeLogAsync(CreateScrapeLogDto log, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("v1/scrape-logs", log, ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            response = await RetryAfterAsync(response, () => _http.PostAsJsonAsync("v1/scrape-logs", log, ct), ct);
        await EnsureSuccessAsync(response, "post scrape log", ct);
    }

    private static async Task<HttpResponseMessage> RetryAfterAsync(
        HttpResponseMessage response,
        Func<Task<HttpResponseMessage>> retry,
        CancellationToken ct)
    {
        var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5);
        if (delay > TimeSpan.FromMinutes(1))
            delay = TimeSpan.FromMinutes(1);
        response.Dispose();
        await Task.Delay(delay, ct);
        return await retry();
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string action, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"Failed to {action} ({(int)response.StatusCode}): {body}");
    }
}