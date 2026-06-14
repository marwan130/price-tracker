namespace PriceTracker.Scraper.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceTracker.Scraper.Api;
using PriceTracker.Scraper.Configuration;
using PriceTracker.Scraper.Scraping;

public class ScrapeWorker : BackgroundService
{
    private readonly IPriceTrackerApiClient _apiClient;
    private readonly IHttpClientFactory     _httpClientFactory;
    private readonly IPriceExtractor        _priceExtractor;
    private readonly ScraperOptions         _options;
    private readonly ILogger<ScrapeWorker>  _logger;

    public ScrapeWorker(
        IPriceTrackerApiClient apiClient,
        IHttpClientFactory     httpClientFactory,
        IPriceExtractor        priceExtractor,
        IOptions<ScraperOptions> options,
        ILogger<ScrapeWorker>  logger)
    {
        _apiClient         = apiClient;
        _httpClientFactory = httpClientFactory;
        _priceExtractor    = priceExtractor;
        _options           = options.Value;
        _logger            = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scraper worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunScrapeCycleAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
        }
    }

    private async Task RunScrapeCycleAsync(CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("Scrape cycle started at {Time}", startedAt);

        var listings = await _apiClient.GetActiveListingsAsync(ct);
        if (listings.Count == 0)
        {
            _logger.LogWarning("No active listings returned from API");
            return;
        }

        var pageClient = _httpClientFactory.CreateClient("page-fetcher");
        var succeeded  = 0;
        var failed     = 0;
        string? lastError = null;

        foreach (var listing in listings)
        {
            if (_options.DelayBetweenListingsMs > 0)
                await Task.Delay(_options.DelayBetweenListingsMs, ct);

            try
            {
                await ScrapeListingAsync(pageClient, listing, ct);
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                lastError = ex.Message;
                _logger.LogError(ex, "Failed to scrape listing {ListingId} ({Url})", listing.ListingId, listing.ProductUrl);

                await TryPostListingFailureLogAsync(listing, startedAt, ex.Message, ct);
            }
        }

        var status = failed == 0 ? "Success" : succeeded == 0 ? "Failed" : "Partial";

        try
        {
            await _apiClient.PostScrapeLogAsync(new CreateScrapeLogDto
            {
                StoreId      = listings[0].StoreId,
                ListingId    = null,
                Status       = status,
                ErrorMessage = failed > 0 ? lastError : null,
                ItemsScraped = succeeded,
                StartedAt    = startedAt,
                FinishedAt   = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post scrape cycle log");
        }

        _logger.LogInformation(
            "Scrape cycle finished: {Succeeded} succeeded, {Failed} failed",
            succeeded,
            failed);
    }

    private async Task ScrapeListingAsync(
        HttpClient       pageClient,
        ScrapeListingDto listing,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, listing.ProductUrl);
        using var response = await pageClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);
        var result = await _priceExtractor.ExtractAsync(html, listing.CurrencyCode, ct)
            ?? throw new InvalidOperationException("Could not extract price from page.");

        await _apiClient.PostPriceRecordAsync(new CreatePriceRecordDto
        {
            ListingId    = listing.ListingId,
            Price        = result.Price,
            CurrencyCode = result.CurrencyCode,
            RecordedAt   = DateTime.UtcNow
        }, ct);

        _logger.LogInformation(
            "Scraped listing {ListingId}: {Price} {Currency}",
            listing.ListingId,
            result.Price,
            result.CurrencyCode);
    }

    private async Task TryPostListingFailureLogAsync(
        ScrapeListingDto listing,
        DateTime         startedAt,
        string           error,
        CancellationToken ct)
    {
        try
        {
            await _apiClient.PostScrapeLogAsync(new CreateScrapeLogDto
            {
                StoreId      = listing.StoreId,
                ListingId    = listing.ListingId,
                Status       = "Failed",
                ErrorMessage = error,
                ItemsScraped = 0,
                StartedAt    = startedAt,
                FinishedAt   = DateTime.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post scrape log for listing {ListingId}", listing.ListingId);
        }
    }
}
