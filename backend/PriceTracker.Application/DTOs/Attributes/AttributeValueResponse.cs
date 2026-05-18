namespace PriceTracker.Application.DTOs.Attributes;

public class AttributeValueResponse
{
    public long   AttributeValueId { get; set; }
    public long   AttributeTypeId  { get; set; }
    public string Type             { get; set; } = string.Empty;   
    public string Value            { get; set; } = string.Empty;   
}