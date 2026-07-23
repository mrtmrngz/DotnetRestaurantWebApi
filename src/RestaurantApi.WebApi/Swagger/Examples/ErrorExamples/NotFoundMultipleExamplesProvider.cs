using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;

public class NotFoundMultipleExamplesProvider: IMultipleExamplesProvider<object>
{
    public IEnumerable<SwaggerExample<object>> GetExamples()
    {
        var notFoundErrorProvider = new NotFoundErrorExample();
        yield return SwaggerExample.Create<object>(
            "1. User bulunamadığı Durumlarda (User Not Found)",
            notFoundErrorProvider.GetExamples()
        );
        
        yield return SwaggerExample.Create<object>("2.Aranan İçerik Bulunamadığı Durumlarda", notFoundErrorProvider.GetExamples());
    }
}