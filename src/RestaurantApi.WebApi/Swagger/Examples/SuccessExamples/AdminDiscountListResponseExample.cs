using RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminDiscountListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>> GetExamples()
    {
        var discounts = new List<GetDiscountListQueryResult>
        {
            new GetDiscountListQueryResult
            {
                Id = Guid.Parse("9a5f1c2d-1111-4a2b-8c3d-000000000001"),
                DiscountRate = 10,
                ProductId = Guid.Parse("1f0d9c8b-7a65-4b43-9210-112233445566"),
                ProductName = "Izgara Tavuk",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = DiscountStatus.Active
            },
            new GetDiscountListQueryResult
            {
                Id = Guid.Parse("9a5f1c2d-2222-4a2b-8c3d-000000000002"),
                DiscountRate = 25,
                ProductId = Guid.Parse("3c2d1e0f-4a5b-4968-b7c8-d9e0f1a2b3c4"),
                ProductName = "Mercimek Çorbası",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = DiscountStatus.Upcoming
            },
            new GetDiscountListQueryResult
            {
                Id = Guid.Parse("9a5f1c2d-3333-4a2b-8c3d-000000000003"),
                DiscountRate = 15,
                ProductId = Guid.Parse("4d3e2f1a-5b6c-4078-9a0b-c1d2e3f4a5b6"),
                ProductName = "Mevsim Salata",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-3),
                Status = DiscountStatus.Passive
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>(data: discounts);
    }
}
