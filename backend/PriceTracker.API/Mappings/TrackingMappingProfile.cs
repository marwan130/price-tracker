namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Tracking;
using PriceTracker.Domain.Entities;

public class TrackingMappingProfile : Profile
{
    public TrackingMappingProfile()
    {
        CreateMap<UserProductTracking, TrackingResponse>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.VariantSku,
                opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Sku : null))
            .ForMember(dest => dest.StoreName,
                opt => opt.MapFrom(src => src.Listing != null ? src.Listing.Store.Name : null))
            .ForMember(dest => dest.CurrentPrice,
                opt => opt.Ignore());
    }
}