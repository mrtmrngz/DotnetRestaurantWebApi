using RestaurantApi.Application.Features.Roles.Commands;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.RoleExamples;

// Rol bilgisi güncelleme isteğinin örnek gövdesi.
// RoleId route'tan geldiği için (JsonIgnore/BindNever) örnekte yalnızca gövde alanı bulunur.
public class UpdateRoleInfoRequestExample: IExamplesProvider<UpdateRoleInfoCommand>
{
    public UpdateRoleInfoCommand GetExamples()
    {
        return new UpdateRoleInfoCommand(Name: "KITCHEN");
    }
}
