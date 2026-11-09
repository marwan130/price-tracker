namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Listings;
using PriceTracker.Domain.Entities;

public class ListingMappingProfile : Profile
{
    public ListingMappingProfile()
    {
        CreateMap<StoreProductListing, ListingResponse>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.StoreName,
                opt => opt.MapFrom(src => src.Store.Name))
            .ForMember(dest => dest.MarketCountry,
                opt => opt.MapFrom(src => src.Store.Country));
    }
}
