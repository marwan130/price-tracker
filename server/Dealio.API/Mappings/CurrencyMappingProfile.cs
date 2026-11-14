namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Application.DTOs.Currencies;
using Dealio.Domain.Entities;

public class CurrencyMappingProfile : Profile
{
    public CurrencyMappingProfile()
    {
        CreateMap<Currency, CurrencyResponse>();
        CreateMap<CreateCurrencyRequest, Currency>();
    }
}