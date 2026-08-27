using RestaurantApi.Application.Features.Category.Queries.AdminCategoryListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminCategoryListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>> GetExamples()
    {
        var categories = new List<AdminCategoryListQueryResult>
        {
            new AdminCategoryListQueryResult
            {
                Id = Guid.Parse("a8e3d641-5f21-4d1b-8711-2d7c58e80101"),
                Title = "Çorbalar",
                Slug = "corbalar",
                ImageUrl = "https://s3.eu-central-1.amazonaws.com/restaurant-media/categories/corbalar.webp",
                IsDeleted = false,
            },
            new AdminCategoryListQueryResult
            {
                Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"),
                Title = "Ana Yemekler",
                Slug = "ana-yemekler",
                ImageUrl = "https://s3.eu-central-1.amazonaws.com/restaurant-media/categories/ana-yemekler.webp",
                IsDeleted = false,
            },
            new AdminCategoryListQueryResult
            {
                Id = Guid.Parse("3c91b8a2-2b62-4217-91a3-1815e9e0f33d"),
                Title = "Başlangıçlar & Mezeler",
                Slug = "baslangiclar-ve-mezeler",
                ImageUrl = "https://s3.eu-central-1.amazonaws.com/restaurant-media/categories/baslangiclar-ve-mezeler.webp",
                IsDeleted = false,
            },
            new AdminCategoryListQueryResult
            {
                Id = Guid.Parse("b14d2e7a-9f44-4211-9a2c-74f0285a82e1"),
                Title = "Tatlılar",
                Slug = "tatlilar",
                ImageUrl = "https://s3.eu-central-1.amazonaws.com/restaurant-media/categories/tatlilar.webp",
                IsDeleted = true,
            },
            new AdminCategoryListQueryResult
            {
                Id = Guid.Parse("7e9b04f1-62d3-4e8c-859a-32c0211d12fa"),
                Title = "Soğuk İçecekler",
                Slug = "soguk-icecekler",
                ImageUrl = "https://s3.eu-central-1.amazonaws.com/restaurant-media/categories/soguk-icecekler.webp",
                IsDeleted = true,
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>(data: categories);
    }
}