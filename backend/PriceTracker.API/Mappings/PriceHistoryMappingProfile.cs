namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.PriceHistory;
using PriceTracker.Domain.Entities;

public class PriceHistoryMappingProfile : Profile
{
    public PriceHistoryMappingProfile()
    {
        CreateMap<PriceHistory, PriceRecordResponse>();
        CreateMap<CreatePriceRecordRequest, PriceHistory>();
    }
}