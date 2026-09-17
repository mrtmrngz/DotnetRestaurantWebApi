using RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class
    CategoryDetailResponseExample : IExamplesProvider<GeneralSuccessResponseWithData<CategoryDetailQueryResult>>
{
    public GeneralSuccessResponseWithData<CategoryDetailQueryResult> GetExamples()
    {
        return new GeneralSuccessResponseWithData<CategoryDetailQueryResult>(
            data: new CategoryDetailQueryResult()
            {
                Id = Guid.NewGuid(),
                Title = "Tatlılar",
                Slug = "tatlilar",
                ImageUrl = "http://localhost:9000/images-bucket/media/6fb92237404c4e93b2f54b55b2176eb7.png",
                IsDeleted = false
            }
        );
    }
}