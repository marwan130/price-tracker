namespace PriceTracker.Application.DTOs.Tracking;

public class UpdateTrackingRequest
{
    public decimal TargetPrice { get; set; }
    public bool    NotifyEmail { get; set; }
}