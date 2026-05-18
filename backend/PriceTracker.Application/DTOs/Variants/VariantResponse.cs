namespace PriceTracker.Application.DTOs.Variants;

using PriceTracker.Application.DTOs.Attributes;

public class VariantResponse
{
    public Guid     VariantId  { get; set; }
    public Guid     ProductId  { get; set; }
    public string?  Sku        { get; set; }
    public bool     IsActive   { get; set; }
    public DateTime CreatedAt  { get; set; }
    public IEnumerable<AttributeValueResponse> Attributes { get; set; } = new List<AttributeValueResponse>();
}