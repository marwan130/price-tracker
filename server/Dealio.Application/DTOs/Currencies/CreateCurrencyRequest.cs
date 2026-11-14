namespace PriceTracker.Application.DTOs.Currencies;

public class CreateCurrencyRequest
{
    public string Code   { get; set; } = string.Empty;
    public string Name   { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}