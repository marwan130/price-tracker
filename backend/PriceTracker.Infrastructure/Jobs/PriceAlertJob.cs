namespace PriceTracker.Infrastructure.Jobs;

using Microsoft.Extensions.Logging;
using PriceTracker.Application.Interfaces.Services;

public class PriceAlertJob
{
    private readonly IPriceAlertService     _priceAlertService;
    private readonly ILogger<PriceAlertJob> _logger;

    public PriceAlertJob(IPriceAlertService priceAlertService, ILogger<PriceAlertJob> logger)
    {
        _priceAlertService = priceAlertService;
        _logger            = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Price alert job started");

        try
        {
            await _priceAlertService.EvaluateAllActiveTrackingsAsync();
            _logger.LogInformation("Price alert job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price alert job execution failed");
            throw;
        }
    }
}