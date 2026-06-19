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
    private readonly IPriceExtractor        _priceExtractor;
    private readonly ScraperOptions         _options;
    private readonly ILogger<ScrapeWorker>  _logger;
    private readonly Random                 _random = new();

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
        using var request = new HttpRequestMessage(HttpMethod.Get, listing.ProductUrl);
        
        // Set timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout per request
        
        // Add random user agent
        request.Headers.UserAgent.ParseList(GetRandomUserAgent());
        
        using var response = await pageClient.SendAsync(request, cts.Token);
        
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException("Rate limited by target server", null, statusCode);
            }
            if (statusCode == HttpStatusCode.Forbidden)
            {
                throw new HttpRequestException("Access forbidden by target server", null, statusCode);
            }
            response.EnsureSuccessStatusCode();
        }

        var html = await response.Content.ReadAsStringAsync(ct);
        
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("Received empty HTML response");
        }

        var result = await _priceExtractor.ExtractAsync(html, listing.CurrencyCode, ct)
            ?? throw new InvalidOperationException("Could not extract price from page.");

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

    private string GetRandomUserAgent()
    {
        var userAgents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };
        return userAgents[_random.Next(userAgents.Length)];
    }
}
