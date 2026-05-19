using RestaurantApi.Application.Features.Auth.Commands.Register;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AuthExamples;

public class RegisterRequestExample: IExamplesProvider<RegisterCommand>
{
    public RegisterCommand GetExamples()
    {
        return new RegisterCommand(
            Name: "John",
            Surname: "Doe",
            Email:"john@mail.com",
            Password:"StrongPassword",
            PhoneNumber: "+905441234567"
        );
    }
}