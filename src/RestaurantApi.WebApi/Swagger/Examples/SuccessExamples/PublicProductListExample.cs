using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class PublicProductListExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>> GetExamples()
    {
        var products = new List<PublicProductsListQueryResult>
        {
            new PublicProductsListQueryResult
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
                }
            },
            new PublicProductsListQueryResult
            {
                Id = Guid.Parse("3c2d1e0f-4a5b-4968-b7c8-d9e0f1a2b3c4"),
                Title = "Mercimek Çorbası",
                Slug = "mercimek-corbasi",
                Price = 95,
                CommentCount = 18,
                AvgRate = 4.5,
                CategoryName = "Çorbalar",
                Banner = "https://s3.eu-central-1.amazonaws.com/restaurant-media/products/mercimek-corbasi.webp",
                Discount = null
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>(data: products);
    }
}
