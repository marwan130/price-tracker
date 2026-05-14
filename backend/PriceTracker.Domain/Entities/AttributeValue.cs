namespace PriceTracker.Domain.Entities;

public class AttributeValue
{
    public long   AttributeValueId { get; set; }
    public long   AttributeTypeId  { get; set; }
    public string Value            { get; set; } = string.Empty;

    // Navigation properties
    public AttributeType                    AttributeType      { get; set; } = null!;
    public ICollection<VariantAttribute>    VariantAttributes  { get; set; } = new List<VariantAttribute>();
}