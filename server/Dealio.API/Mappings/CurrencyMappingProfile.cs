namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Currencies;
using PriceTracker.Domain.Entities;

public class CurrencyMappingProfile : Profile
{
    public CurrencyMappingProfile()
    {
        CreateMap<Currency, CurrencyResponse>();
        CreateMap<CreateCurrencyRequest, Currency>();
    }
}