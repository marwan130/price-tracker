namespace PriceTracker.Scraper.Api;

public class CreatePriceRecordDto
{
    public Guid     ListingId    { get; set; }
    public decimal  Price        { get; set; }
    public string   CurrencyCode { get; set; } = string.Empty;
    public decimal? PriceInUsd   { get; set; }
    public DateTime RecordedAt   { get; set; }
}
