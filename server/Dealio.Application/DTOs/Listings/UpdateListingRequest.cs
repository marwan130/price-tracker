namespace PriceTracker.Application.DTOs.Listings;

public class UpdateListingRequest
{
    public string ProductUrl { get; set; } = string.Empty;
    public bool   IsActive   { get; set; }
}