namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Domain.Entities;

public class ScrapeLogMappingProfile : Profile
{
    public ScrapeLogMappingProfile()
    {
        CreateMap<ScrapeLog, ScrapeLog>();
    }
}