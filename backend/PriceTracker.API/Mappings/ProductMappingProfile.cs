namespace PriceTracker.API.Mappings;

using AutoMapper;
using PriceTracker.Application.DTOs.Products;
using PriceTracker.Domain.Entities;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductSummaryResponse>()
            .ForMember(dest => dest.Category,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.PrimaryImage,
                opt => opt.MapFrom(src => src.Images.FirstOrDefault(i => i.IsPrimary) != null
                    ? src.Images.First(i => i.IsPrimary).Url
                    : src.Images.FirstOrDefault() != null
                    ? src.Images.First().Url
                    : null));

        CreateMap<Product, ProductResponse>()
            .ForMember(dest => dest.Category,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.PrimaryImage,
                opt => opt.MapFrom(src => src.Images.FirstOrDefault(i => i.IsPrimary) != null
                    ? src.Images.First(i => i.IsPrimary).Url
                    : src.Images.FirstOrDefault() != null
                    ? src.Images.First().Url
                    : null));

        CreateMap<CreateProductRequest, Product>();
        CreateMap<UpdateProductRequest, Product>();
    }
}