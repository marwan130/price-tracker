namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Domain.Entities;

public class ScrapeLogMappingProfile : Profile
{
    public ScrapeLogMappingProfile()
    {
        CreateMap<ScrapeLog, ScrapeLog>();
    }
}