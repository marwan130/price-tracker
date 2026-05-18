namespace PriceTracker.Application.DTOs.PriceHistory;

public class PriceHistoryFilterRequest
{
    public Guid?     ListingId { get; set; }
    public DateTime? From      { get; set; }
    public DateTime? To        { get; set; }
}