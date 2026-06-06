namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Attributes;
using PriceTracker.Application.DTOs.Variants;
using PriceTracker.Domain.Entities;

public class VariantMappingProfile : Profile
{
    public VariantMappingProfile()
    {
        CreateMap<ProductVariant, VariantResponse>()
            .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src =>
                src.VariantAttributes.Select(va => new AttributeValueResponse
                {
                    AttributeValueId = va.AttributeValue.AttributeValueId,
                    AttributeTypeId  = va.AttributeValue.AttributeTypeId,
                    Type             = va.AttributeValue.AttributeType.Name,
                    Value            = va.AttributeValue.Value
                })));
    }
}