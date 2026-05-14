namespace PriceTracker.Domain.Entities;

public class AttributeType
{
    public long   AttributeTypeId { get; set; }
    public string Name            { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<AttributeValue> Values { get; set; } = new List<AttributeValue>();
}