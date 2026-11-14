namespace Dealio.API.Mappings;

using AutoMapper;
using Dealio.Application.DTOs.Categories;
using Dealio.Domain.Entities;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryResponse>();
        CreateMap<CreateCategoryRequest, Category>();
        CreateMap<UpdateCategoryRequest, Category>();
    }
}
