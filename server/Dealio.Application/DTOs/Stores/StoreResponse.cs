namespace PriceTracker.Application.DTOs.Stores;

using PriceTracker.Domain.Enums;

public class StoreResponse
{
    public Guid       StoreId      { get; set; }
    public string     Name         { get; set; } = string.Empty;
    public string     BaseUrl      { get; set; } = string.Empty;
    public string     Country      { get; set; } = string.Empty;
    public string?    CurrencyCode { get; set; }
    public bool       IsActive     { get; set; }
    public ScraperType ScraperType  { get; set; } = ScraperType.Html;
    public DateTime   CreatedAt    { get; set; }
}