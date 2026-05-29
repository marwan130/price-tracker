namespace PriceTracker.Application.Interfaces.Services;

public interface IPriceAlertService
{
    Task EvaluateAllActiveTrackingsAsync();
    Task EvaluateTrackingAsync(Guid trackingId);
}