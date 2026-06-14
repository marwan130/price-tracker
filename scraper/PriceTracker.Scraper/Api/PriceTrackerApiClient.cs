namespace PriceTracker.Scraper.Api;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceTracker.Scraper.Configuration;

public class PriceTrackerApiClient : IPriceTrackerApiClient
{
    private readonly HttpClient        _http;
    private readonly ILogger<PriceTrackerApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PriceTrackerApiClient(
        HttpClient                    http,
        IOptions<ApiOptions>          apiOptions,
        ILogger<PriceTrackerApiClient> logger)
    {
        _http   = http;
        _logger = logger;

        var api = apiOptions.Value;
        _http.BaseAddress = new Uri(api.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add("X-Internal-Key", api.InternalKey);
    }

    public async Task<IReadOnlyList<ScrapeListingDto>> GetActiveListingsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("v1/internal/listings/active", ct);

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

    public async Task PostPriceRecordAsync(CreatePriceRecordDto record, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("v1/price-history", record, ct);
        await EnsureSuccessAsync(response, "post price record", ct);
    }

    public async Task PostScrapeLogAsync(CreateScrapeLogDto log, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("v1/scrape-logs", log, ct);
        await EnsureSuccessAsync(response, "post scrape log", ct);
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
