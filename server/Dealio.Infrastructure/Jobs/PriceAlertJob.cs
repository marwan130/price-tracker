namespace Dealio.Infrastructure.Jobs;

using Microsoft.Extensions.Logging;
using Dealio.Application.Interfaces.Services;

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

        try
        {
            await _priceAlertService.EvaluateAllActiveTrackingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price alert evaluation failed");
            throw;
        }
    }
}