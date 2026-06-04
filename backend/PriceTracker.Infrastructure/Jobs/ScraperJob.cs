namespace PriceTracker.Infrastructure.Jobs;

using Microsoft.Extensions.Logging;
using PriceTracker.Application.Interfaces.Services;

public class ScraperJob
{
    private readonly IScraperService  _scraperService;
    private readonly ILogger<ScraperJob> _logger;

    public ScraperJob(IScraperService scraperService, ILogger<ScraperJob> logger)
    {
        _scraperService = scraperService;
        _logger         = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Scraper job started at {Time}", DateTime.UtcNow);

        try
        {
            await _scraperService.ScrapeAllActiveListingsAsync();
            _logger.LogInformation("Scraper job completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scraper job failed at {Time}", DateTime.UtcNow);
            throw;
        }
    }
}