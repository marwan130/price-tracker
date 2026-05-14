namespace PriceTracker.Domain.Entities;

public class Currency
{
    public string Code   { get; set; } = string.Empty;
    public string Name   { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Store>               Stores         { get; set; } = new List<Store>();
    public ICollection<PriceHistory>        PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<UserProductTracking> Trackings      { get; set; } = new List<UserProductTracking>();
}