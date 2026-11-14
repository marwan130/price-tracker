namespace PriceTracker.Application.DTOs.Products;

public class ProductSummaryResponse
{
    public Guid     ProductId    { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string?  Brand        { get; set; }
    public string?  Category     { get; set; }
    public string?  PrimaryImage { get; set; }
    public decimal? LowestPrice  { get; set; }
    public string?  Currency     { get; set; }
    public string?  CurrencyCode { get; set; }
    public int      StoreCount   { get; set; }

    // Previous observation of the house offer behind LowestPrice; shown as
    // "previous price" in feeds. Null when no comparable previous
    // observation exists (or it mixes currencies).
    public decimal?  PreviousPrice       { get; set; }
    public DateTime? PreviousRecordedAt  { get; set; }
    public decimal?  PriceChangePercent  { get; set; } // signed; negative = price decreased
}