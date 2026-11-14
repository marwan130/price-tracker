namespace Dealio.Application.DTOs.Products;

/// <summary>Round-trip result of live image resolution; ImageUrl is null when the retailer page exposes none.</summary>
public class ImageResolveResponse
{
    public string? ImageUrl { get; set; }
}
