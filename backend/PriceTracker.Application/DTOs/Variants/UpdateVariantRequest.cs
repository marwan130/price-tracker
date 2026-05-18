namespace PriceTracker.Application.DTOs.Variants;

public class UpdateVariantRequest
{
    public string? Sku      { get; set; }
    public bool    IsActive { get; set; }
}