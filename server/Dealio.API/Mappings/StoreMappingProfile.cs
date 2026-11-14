namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Application.DTOs.Stores;
using Dealio.Domain.Entities;

public class StoreMappingProfile : Profile
{
    public StoreMappingProfile()
    {
        CreateMap<Store, StoreResponse>();
        CreateMap<CreateStoreRequest, Store>();
        CreateMap<UpdateStoreRequest, Store>();
    }
}