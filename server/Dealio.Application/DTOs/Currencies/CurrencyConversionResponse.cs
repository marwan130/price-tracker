namespace PriceTracker.Application.DTOs.Currencies;

public sealed class CurrencyConversionResponse
{
    public string TargetCurrency { get; set; } = string.Empty;
    public DateTime? RateUpdatedAtUtc { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string AttributionUrl { get; set; } = string.Empty;
    public IReadOnlyList<CurrencyConversionResult> Items { get; set; } = [];
}

public sealed class CurrencyConversionResult
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal? ConvertedAmount { get; set; }
    public decimal? ConversionRate { get; set; }
}
