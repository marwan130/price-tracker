namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Application.DTOs.PriceHistory;
using Dealio.Domain.Entities;

public class PriceHistoryMappingProfile : Profile
{
    public PriceHistoryMappingProfile()
    {
        CreateMap<PriceHistory, PriceRecordResponse>();
    }
}
