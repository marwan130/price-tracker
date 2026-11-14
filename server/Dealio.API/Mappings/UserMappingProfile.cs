namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Application.DTOs.Users;
using Dealio.Domain.Entities;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserResponse>();
    }
}