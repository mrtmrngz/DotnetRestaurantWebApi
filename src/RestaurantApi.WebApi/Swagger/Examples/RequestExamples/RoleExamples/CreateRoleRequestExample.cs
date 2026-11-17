using RestaurantApi.Application.Features.Roles.Commands;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.RoleExamples;

// Rol oluşturma isteğinin örnek gövdesi: yalnızca rol adı gönderilir.
public class CreateRoleRequestExample: IExamplesProvider<CreateRoleCommand>
{
    public CreateRoleCommand GetExamples()
    {
        return new CreateRoleCommand(Name: "KITCHEN");
    }
}
