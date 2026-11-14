namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Application.DTOs.Tracking;
using Dealio.Domain.Entities;

public class TrackingMappingProfile : Profile
{
    public TrackingMappingProfile()
    {
        CreateMap<UserProductTracking, TrackingResponse>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Product.Category != null ? src.Product.Category.Name : null))
            .ForMember(dest => dest.MarketCountry,
                opt => opt.MapFrom(src => src.Listing != null ? src.Listing.Store.Country : null))
            .ForMember(dest => dest.StoreName,
                opt => opt.MapFrom(src => src.Listing != null ? src.Listing.Store.Name : null))
            .ForMember(dest => dest.CurrentPrice,
                opt => opt.Ignore());
    }
}
