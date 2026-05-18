namespace PriceTracker.Application.DTOs.Listings;

public class CreateListingRequest
{
    public Guid   ProductId  { get; set; }
    public Guid   VariantId  { get; set; }
    public Guid   StoreId    { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
}