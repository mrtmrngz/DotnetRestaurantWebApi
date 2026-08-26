using AutoMapper;
using RestaurantApi.Application.Features.Category.Queries.GetCategoriesQuery;

namespace RestaurantApi.Application.Features.Profiles;

public class CategoriesMappingProfile : Profile
{
    public CategoriesMappingProfile()
    {
        // Get categories for public routes
        CreateMap<Domain.Entities.Category, GetCategoriesQueryResult>()
            .ForMember(dest => dest.Id, opt =>
                opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt =>
                opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Slug, opt =>
                opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.ImageUrl, opt =>
                opt.MapFrom(src => src.Media != null ? src.Media.Url : string.Empty)
            );
    }
}