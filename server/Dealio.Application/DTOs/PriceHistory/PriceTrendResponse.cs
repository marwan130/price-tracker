namespace Dealio.Application.DTOs.PriceHistory;

using Dealio.Domain.Enums;

public class PriceTrendResponse
{
    public Guid                            ListingId      { get; set; }
    public string                          ProductName    { get; set; } = string.Empty;
    public string                          StoreName      { get; set; } = string.Empty;
    public DateTime                        From           { get; set; }
    public DateTime                        To             { get; set; }
    public decimal                         LowestPrice    { get; set; }
    public decimal                         HighestPrice   { get; set; }
    public decimal                         AveragePrice   { get; set; }
    public decimal                         CurrentPrice   { get; set; }
    public int                             PriceDropCount { get; set; }
    public PriceTrend                      Trend          { get; set; }
    public IEnumerable<PriceDataPoint>     DataPoints     { get; set; } = new List<PriceDataPoint>();
}

public class PriceDataPoint
{
    public DateTime RecordedAt { get; set; }
    public decimal  Price      { get; set; }
}
