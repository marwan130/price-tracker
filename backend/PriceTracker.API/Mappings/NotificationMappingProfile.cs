namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Notifications;
using PriceTracker.Domain.Entities;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationResponse>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Tracking.Product.Name))
            .ForMember(dest => dest.VariantSku,
                opt => opt.MapFrom(src => src.Tracking.Variant != null ? src.Tracking.Variant.Sku : null))
            .ForMember(dest => dest.StoreName,
                opt => opt.MapFrom(src => src.Tracking.Listing != null ? src.Tracking.Listing.Store.Name : null))
            .ForMember(dest => dest.CurrencyCode,
                opt => opt.MapFrom(src => src.Tracking.CurrencyCode));
    }
}