namespace PriceTracker.Application.DTOs.Attributes;

public class AttributeTypeResponse
{
    public long   AttributeTypeId { get; set; }
    public string Name            { get; set; } = string.Empty;
    public IEnumerable<AttributeValueResponse> Values { get; set; } = [];
}