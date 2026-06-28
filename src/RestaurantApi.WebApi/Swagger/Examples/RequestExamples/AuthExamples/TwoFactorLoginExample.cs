using RestaurantApi.Application.Features.Auth.Commands.TwoFactorLogin;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AuthExamples;

public class TwoFactorLoginExample: IExamplesProvider<TwoFactorLoginCommand>
{
    public TwoFactorLoginCommand GetExamples()
    {
        return new TwoFactorLoginCommand(Otp: "123456");
    }
}