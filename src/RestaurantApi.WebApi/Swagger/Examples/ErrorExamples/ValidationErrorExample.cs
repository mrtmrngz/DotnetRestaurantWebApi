using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;

public class ValidationErrorExample: IExamplesProvider<ValidationErrorResponse>
{
    public ValidationErrorResponse GetExamples()
    {
        return new ValidationErrorResponse
        {
            Code = Codes.VALIDATION_ERROR,
            Message = "Validasyon hataları oluştu.",
            StatusCode = 400,
            Errors = new List<ErrorList>()
            {
                new ErrorList(){Field = "Hata başlığı", Message = "Hata mesajı."}
            }
        };
    }
}