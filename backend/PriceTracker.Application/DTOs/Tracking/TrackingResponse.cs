namespace PriceTracker.Application.DTOs.Tracking;

public class TrackingResponse
{
    public Guid     TrackingId    { get; set; }
    public Guid     UserId        { get; set; }
    public Guid     ProductId     { get; set; }
    public string   ProductName   { get; set; } = string.Empty;
    public Guid?    VariantId     { get; set; }
    public string?  VariantSku    { get; set; }
    public Guid?    ListingId     { get; set; }
    public string?  StoreName     { get; set; }
    public decimal  TargetPrice   { get; set; }
    public string   CurrencyCode  { get; set; } = string.Empty;
    public decimal? CurrentPrice  { get; set; }
    public bool     IsActive      { get; set; }
    public bool     NotifyEmail   { get; set; }
    public DateTime CreatedAt     { get; set; }
}