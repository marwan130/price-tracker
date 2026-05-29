namespace PriceTracker.Application.Services;

using PriceTracker.Application.DTOs.Currencies;
using PriceTracker.Application.Interfaces.Repositories;
using PriceTracker.Application.Interfaces.Services;
using PriceTracker.Domain.Entities;
using PriceTracker.Domain.Exceptions;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _currencyRepository;

    public CurrencyService(ICurrencyRepository currencyRepository)
        => _currencyRepository = currencyRepository;

    public async Task<IEnumerable<CurrencyResponse>> GetAllAsync()
    {
        var currencies = await _currencyRepository.GetAllAsync();
        return currencies.Select(MapToResponse);
    }

    public async Task<CurrencyResponse> GetByCodeAsync(string code)
    {
        var currency = await _currencyRepository.GetByCodeAsync(code)
            ?? throw new NotFoundException(nameof(Currency), code);

        return MapToResponse(currency);
    }

    public async Task<CurrencyResponse> CreateAsync(CreateCurrencyRequest request)
    {
        if (await _currencyRepository.ExistsByCodeAsync(request.Code))
            throw new ConflictException($"Currency '{request.Code}' already exists.");

        var currency = new Currency
        {
            Code   = request.Code.ToUpper(),
            Name   = request.Name,
            Symbol = request.Symbol
        };

        await _currencyRepository.AddAsync(currency);
        return MapToResponse(currency);
    }

    public async Task DeleteAsync(string code)
    {
        var currency = await _currencyRepository.GetByCodeAsync(code)
            ?? throw new NotFoundException(nameof(Currency), code);

        await _currencyRepository.DeleteAsync(currency);
    }

    private static CurrencyResponse MapToResponse(Currency currency) => new()
    {
        Code   = currency.Code,
        Name   = currency.Name,
        Symbol = currency.Symbol
    };
}