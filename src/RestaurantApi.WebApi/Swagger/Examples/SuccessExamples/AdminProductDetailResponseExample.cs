using RestaurantApi.Application.Features.Products.Queries.AdminProductDetailQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminProductDetailResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<AdminProductDetailQueryResult>>
{
    public GeneralSuccessResponseWithData<AdminProductDetailQueryResult> GetExamples()
    {
        return new GeneralSuccessResponseWithData<AdminProductDetailQueryResult>(
            data: new AdminProductDetailQueryResult
            {
                Id = Guid.Parse("1f0d9c8b-7a65-4b43-9210-112233445566"),
                Title = "Izgara Tavuk",
                Description = "Izgara tavuk, mevsim yeşillikleri ile servis edilir.",
                Slug = "izgara-tavuk",
                Price = 320.50m,
                CommentCount = 42,
                AvgRate = 4.7,
                CategoryName = "Ana Yemekler",
                Images = new List<ProductDetailImageList>
                {
                    new ProductDetailImageList
                    {
                        Id = Guid.Parse("5e4f3a2b-1c0d-4e9f-8a7b-6c5d4e3f2a1b"),
                        PublicId = "media/izgara-tavuk-1.webp",
                        Url = "https://s3.eu-central-1.amazonaws.com/restaurant-media/products/izgara-tavuk-1.webp",
                        SortOrder = 0,
                        IsMain = true
                    },
                    new ProductDetailImageList
                    {
                        Id = Guid.Parse("6f5e4d3c-2b1a-4f0e-9d8c-7b6a5f4e3d2c"),
                        PublicId = "media/izgara-tavuk-2.webp",
                        Url = "https://s3.eu-central-1.amazonaws.com/restaurant-media/products/izgara-tavuk-2.webp",
                        SortOrder = 1,
                        IsMain = false
                    }
                },
                Discount = new ProductActiveDiscount
                {
                    Id = Guid.Parse("2a1b3c4d-5e6f-4071-8293-a4b5c6d7e8f9"),
                    DiscountRate = 10
                },
                IsDeleted = false,
                TotalSold = 128
            }
        );
    }
}
