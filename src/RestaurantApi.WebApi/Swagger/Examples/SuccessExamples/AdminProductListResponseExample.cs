using RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminProductListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>> GetExamples()
    {
        var products = new List<AdminProductListQueryResult>
        {
            new AdminProductListQueryResult
            {
                Id = Guid.Parse("1f0d9c8b-7a65-4b43-9210-112233445566"),
                Title = "Izgara Tavuk",
                Slug = "izgara-tavuk",
                Price = 320.50m,
                CommentCount = 42,
                AvgRate = 4.7,
                CategoryName = "Ana Yemekler",
                Banner = "https://s3.eu-central-1.amazonaws.com/restaurant-media/products/izgara-tavuk.webp",
                Discount = new ProductActiveDiscount
                {
                    Id = Guid.Parse("2a1b3c4d-5e6f-4071-8293-a4b5c6d7e8f9"),
                    DiscountRate = 10
                },
                IsDeleted = false
            },
            new AdminProductListQueryResult
            {
                Id = Guid.Parse("3c2d1e0f-4a5b-4968-b7c8-d9e0f1a2b3c4"),
                Title = "Mercimek Çorbası",
                Slug = "mercimek-corbasi",
                Price = 95,
                CommentCount = 18,
                AvgRate = 4.5,
                CategoryName = "Çorbalar",
                Banner = "https://s3.eu-central-1.amazonaws.com/restaurant-media/products/mercimek-corbasi.webp",
                Discount = null,
                IsDeleted = false
            },
            new AdminProductListQueryResult
            {
                Id = Guid.Parse("4d3e2f1a-5b6c-4078-9a0b-c1d2e3f4a5b6"),
                Title = "Mevsim Salata",
                Slug = "mevsim-salata",
                Price = 120,
                CommentCount = 7,
                AvgRate = 4.2,
                CategoryName = "Başlangıçlar & Mezeler",
                Banner = "https://s3.eu-central-1.amazonaws.com/restaurant-media/products/mevsim-salata.webp",
                Discount = null,
                IsDeleted = true
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>(data: products);
    }
}
