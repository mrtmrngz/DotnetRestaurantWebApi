using RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AuthExamples;

public class ForgotPasswordVerifyExample: IExamplesProvider<ForgotPasswordVerifyCommand>
{
    public ForgotPasswordVerifyCommand GetExamples()
    {
        return new ForgotPasswordVerifyCommand(Email: "john@gmail.com");
    }
}