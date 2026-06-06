namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Attributes;
using PriceTracker.Domain.Entities;

public class AttributeTypeMappingProfile : Profile
{
    public AttributeTypeMappingProfile()
    {
        CreateMap<AttributeType, AttributeTypeResponse>()
            .ForMember(dest => dest.Values,
                opt => opt.MapFrom(src => src.Values));

        CreateMap<AttributeValue, AttributeValueResponse>()
            .ForMember(dest => dest.Type,
                opt => opt.MapFrom(src => src.AttributeType.Name));
    }
}