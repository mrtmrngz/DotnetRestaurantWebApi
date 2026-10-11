using RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminDiscountDetailResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>
{
    public GeneralSuccessResponseWithData<GetDiscountDetailQueryResult> GetExamples()
    {
        return new GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>(
            data: new GetDiscountDetailQueryResult
            {
                Id = Guid.Parse("9a5f1c2d-1111-4a2b-8c3d-000000000001"),
                DiscountRate = 10,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = DiscountStatus.Active,
                ProductId = Guid.Parse("1f0d9c8b-7a65-4b43-9210-112233445566"),
                ProductName = "Izgara Tavuk",
                ProductPrice = 320.50m,
                DiscountedPrice = 288.45m
            }
        );
    }
}
