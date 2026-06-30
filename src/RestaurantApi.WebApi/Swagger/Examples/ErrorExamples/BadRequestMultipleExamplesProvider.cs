using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;

public class BadRequestMultipleExamplesProvider: IMultipleExamplesProvider<object>
{
    public IEnumerable<SwaggerExample<object>> GetExamples()
    {
        var validationProvider = new ValidationErrorExample();
        yield return SwaggerExample.Create<object>(
            "1. Validasyon Hataları (Validation Errors)",
            validationProvider.GetExamples()
        );

        var businessProvider = new BadRequestErrorExample();
        yield return SwaggerExample.Create<object>("2.İş mantığı hataları", businessProvider.GetExamples());
    }
}