namespace Dealio.Application.DTOs.Currencies;

/// <summary>
/// One USD-based rate snapshot published by the conversion provider. Client code
/// derives any cross rate locally via target / source, so every market's listed
/// price can be shown in the currency chosen in settings without extra calls.
/// </summary>
public sealed class CurrencyRatesResponse
{
    public string Base { get; set; } = "USD";
    public DateTime RateUpdatedAtUtc { get; set; }
    public DateTime NextUpdateAtUtc { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string AttributionUrl { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, decimal> Rates { get; set; } = new Dictionary<string, decimal>();
}
