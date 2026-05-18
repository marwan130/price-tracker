namespace PriceTracker.Application.DTOs.Variants;

public class CreateVariantRequest
{
    public string?      Sku                { get; set; }
    public List<long>   AttributeValueIds  { get; set; } = [];
}