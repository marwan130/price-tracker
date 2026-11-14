namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Users;
using PriceTracker.Domain.Entities;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserResponse>();
    }
}