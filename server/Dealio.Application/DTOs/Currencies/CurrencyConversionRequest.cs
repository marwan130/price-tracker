namespace Dealio.Application.DTOs.Currencies;

public sealed class CurrencyConversionRequest
{
    public string TargetCurrency { get; set; } = string.Empty;
    public List<CurrencyConversionInput> Items { get; set; } = [];
}

public sealed class CurrencyConversionInput
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
}
