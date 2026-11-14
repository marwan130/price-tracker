namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Currencies;

public interface ICurrencyConversionService
{
    Task<CurrencyConversionResponse> ConvertAsync(CurrencyConversionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the current provider rate snapshot so clients can convert locally.</summary>
    Task<CurrencyRatesResponse> GetRateSnapshotAsync(CancellationToken cancellationToken = default);
}
