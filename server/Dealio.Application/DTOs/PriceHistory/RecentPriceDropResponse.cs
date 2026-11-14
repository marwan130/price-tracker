namespace PriceTracker.Application.DTOs.PriceHistory;

public class RecentPriceDropResponse
{
    public Guid     ListingId     { get; set; }
    public Guid     ProductId     { get; set; }
    public string   ProductName   { get; set; } = string.Empty;
    public string   StoreName     { get; set; } = string.Empty;
    public decimal  PreviousPrice { get; set; }
    public decimal  CurrentPrice  { get; set; }
    public decimal  DropPercent   { get; set; }
    public string   CurrencyCode  { get; set; } = string.Empty;
    public DateTime RecordedAt    { get; set; }
}
