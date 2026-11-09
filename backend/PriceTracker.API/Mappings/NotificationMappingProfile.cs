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
            .ForMember(dest => dest.StoreName,
                opt => opt.MapFrom(src => src.PriceHistory.Listing.Store.Name))
            .ForMember(dest => dest.ListingId,
                opt => opt.MapFrom(src => src.PriceHistory.ListingId))
            .ForMember(dest => dest.TriggeredCurrencyCode,
                opt => opt.MapFrom(src => src.PriceHistory.CurrencyCode))
            .ForMember(dest => dest.TargetCurrencyCode,
                opt => opt.MapFrom(src => src.Tracking.CurrencyCode))
            .ForMember(dest => dest.CurrencyCode,
                opt => opt.MapFrom(src => src.Tracking.CurrencyCode));
    }
}
