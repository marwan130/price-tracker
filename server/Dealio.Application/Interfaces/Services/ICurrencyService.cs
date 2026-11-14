namespace Dealio.Application.Interfaces.Services;

using Dealio.Application.DTOs.Currencies;

public interface ICurrencyService
{
    Task<IEnumerable<CurrencyResponse>> GetAllAsync();
    Task<CurrencyResponse>              GetByCodeAsync(string code);
    Task<CurrencyResponse>              CreateAsync(CreateCurrencyRequest request);
    Task                                DeleteAsync(string code);
}