namespace PriceTracker.Application.DTOs.PriceHistory;

public class PriceRecordResponse
{
    public long     Id           { get; set; }
    public Guid     ListingId    { get; set; }
    public decimal  Price        { get; set; }
    public string   CurrencyCode { get; set; } = string.Empty;
    public decimal? PriceInUsd   { get; set; }
    public DateTime RecordedAt   { get; set; }
    public DateTime ScrapedAt    { get; set; }
}