namespace Dealio.Application.DTOs.Tracking;

public class CreateTrackingRequest
{
    public Guid     ProductId    { get; set; }
    public Guid?    ListingId    { get; set; }    
    public decimal  TargetPrice  { get; set; }
    public string   CurrencyCode { get; set; } = string.Empty;
    public bool     NotifyEmail  { get; set; } = true;
    public bool     NotifyFurtherDecreaseEmail { get; set; } = true;
    public bool     IsActive { get; set; } = true;
}
