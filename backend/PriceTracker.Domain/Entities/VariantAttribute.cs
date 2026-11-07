namespace PriceTracker.Domain.Entities;

public class VariantAttribute
{
    public Guid VariantId        { get; set; }
    public long AttributeValueId { get; set; }
    public ProductVariant  Variant        { get; set; } = null!;
    public AttributeValue  AttributeValue { get; set; } = null!;
}