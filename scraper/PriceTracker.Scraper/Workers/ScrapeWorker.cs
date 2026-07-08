namespace PriceTracker.Scraper.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceTracker.Scraper.Api;
using PriceTracker.Scraper.Configuration;
using PriceTracker.Scraper.Scraping;
using System.Net;

public class ScrapeWorker : BackgroundService
{
    private readonly IPriceTrackerApiClient _apiClient;
    private readonly IHttpClientFactory     _httpClientFactory;
    private readonly StoreScraperFactory    _scraperFactory;
    private readonly ScraperOptions         _options;
    private readonly ILogger<ScrapeWorker>  _logger;
    private readonly Random                 _random = new();

    public ScrapeWorker(
        IPriceTrackerApiClient apiClient,
        IHttpClientFactory     httpClientFactory,
        StoreScraperFactory    scraperFactory,
        IOptions<ScraperOptions> options,
        ILogger<ScrapeWorker>  logger)
    {
        _apiClient         = apiClient;
        _httpClientFactory = httpClientFactory;
        _scraperFactory    = scraperFactory;
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

        _logger.LogInformation("Fetched {Count} listings to scrape", listings.Count);

        var pageClient = _httpClientFactory.CreateClient("page-fetcher");
        var succeeded  = 0;
        var failed     = 0;
        string? lastError = null;

        foreach (var listing in listings)
        {
            if (_options.DelayBetweenListingsMs > 0)
            {
                var jitter = _random.Next(0, (int)(_options.DelayBetweenListingsMs * 0.2));
                await Task.Delay(_options.DelayBetweenListingsMs + jitter, ct);
            }

            try
            {
                await ScrapeListingWithRetryAsync(pageClient, listing, ct);
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

    private async Task ScrapeListingWithRetryAsync(
        HttpClient       pageClient,
        ScrapeListingDto listing,
        CancellationToken ct)
    {
        const int maxRetries = 3;
        int attempt = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);

        while (attempt < maxRetries)
        {
            try
            {
                await ScrapeListingAsync(pageClient, listing, ct);
                return;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries - 1)
            {
                attempt++;
                _logger.LogWarning(
                    "Attempt {Attempt}/{MaxRetries} failed for listing {ListingId}: {Error}. Retrying in {Delay}s...",
                    attempt,
                    maxRetries,
                    listing.ListingId,
                    ex.Message,
                    delay.TotalSeconds);
                
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30)); // Exponential backoff with max 30s
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                attempt++;
                _logger.LogWarning(
                    "Attempt {Attempt}/{MaxRetries} failed for listing {ListingId}: {Error}. Retrying in {Delay}s...",
                    attempt,
                    maxRetries,
                    listing.ListingId,
                    ex.Message,
                    delay.TotalSeconds);
                
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }

        // Last attempt - let it throw
        await ScrapeListingAsync(pageClient, listing, ct);
    }

    private async Task ScrapeListingAsync(
        HttpClient       pageClient,
        ScrapeListingDto listing,
        CancellationToken ct)
    {
        // Get the appropriate scraper for this store
        var scraper = _scraperFactory.GetScraper(listing.ScraperType);
        
        // Check if store is marked as unsupported
        if (listing.ScraperType == "Unsupported")
        {
            throw new InvalidOperationException($"Store {listing.StoreName} is marked as unsupported (likely due to CAPTCHA).");
        }

        var result = await scraper.ScrapeAsync(listing.ProductUrl, listing.CurrencyCode, ct);
        
        // Check for CAPTCHA block
        if (result?.IsCaptchaBlocked == true)
        {
            throw new InvalidOperationException($"Store {listing.StoreName} is blocked by CAPTCHA: {result.BlockReason}. Mark as unsupported in admin panel.");
        }

        if (result is null)
        {
            throw new InvalidOperationException("Could not extract price from page.");
        }

        if (result.Price <= 0)
        {
            throw new InvalidOperationException($"Invalid price extracted: {result.Price}");
        }

        await _apiClient.PostPriceRecordAsync(new CreatePriceRecordDto
        {
            ListingId    = listing.ListingId,
            Price        = result.Price,
            CurrencyCode = result.CurrencyCode,
            RecordedAt   = DateTime.UtcNow
        }, ct);

        _logger.LogInformation(
            "Scraped listing {ListingId} using {ScraperType}: {Price} {Currency}",
            listing.ListingId,
            scraper.ScraperType,
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
