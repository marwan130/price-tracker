namespace PriceTracker.Application.DTOs.Stores;

public class UpdateStoreRequest
{
    public string  Name         { get; set; } = string.Empty;
    public string  BaseUrl      { get; set; } = string.Empty;
    public string  Country      { get; set; } = string.Empty;
    public string? CurrencyCode { get; set; }
    public bool    IsActive     { get; set; }
}