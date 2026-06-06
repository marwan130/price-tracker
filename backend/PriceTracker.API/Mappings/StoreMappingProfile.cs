namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Stores;
using PriceTracker.Domain.Entities;

public class StoreMappingProfile : Profile
{
    public StoreMappingProfile()
    {
        CreateMap<Store, StoreResponse>();
        CreateMap<CreateStoreRequest, Store>();
        CreateMap<UpdateStoreRequest, Store>();
    }
}