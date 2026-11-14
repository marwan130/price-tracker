namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Categories;
using PriceTracker.Domain.Entities;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryResponse>();
        CreateMap<CreateCategoryRequest, Category>();
        CreateMap<UpdateCategoryRequest, Category>();
    }
}
