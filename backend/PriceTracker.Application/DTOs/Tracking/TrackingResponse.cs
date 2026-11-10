namespace PriceTracker.Application.DTOs.Tracking;

public class TrackingResponse
{
    public Guid     TrackingId    { get; set; }
    public Guid     UserId        { get; set; }
    public Guid     ProductId     { get; set; }
    public string   ProductName   { get; set; } = string.Empty;
    public string?  CategoryName  { get; set; }
    public string?  MarketCountry { get; set; }
    public Guid?    ListingId     { get; set; }
    public string?  StoreName     { get; set; }
    public decimal  TargetPrice   { get; set; }
    public string   CurrencyCode  { get; set; } = string.Empty;
    public decimal? CurrentPrice  { get; set; }
    public string?  CurrentPriceCurrencyCode { get; set; }
    public decimal? CurrentPriceInTargetCurrency { get; set; }
    public decimal? CurrentPriceInDisplayCurrency { get; set; }
    public decimal? TargetPriceInDisplayCurrency { get; set; }
    public decimal? PreviousPriceInDisplayCurrency { get; set; }
    public decimal? PriceDropPercent { get; set; }
    public DateTime? CurrentPriceUpdatedAt { get; set; }
    public bool     IsActive      { get; set; }
    public bool     NotifyEmail   { get; set; }
    public bool     NotifyFurtherDecreaseEmail { get; set; }
    public DateTime CreatedAt     { get; set; }
}
