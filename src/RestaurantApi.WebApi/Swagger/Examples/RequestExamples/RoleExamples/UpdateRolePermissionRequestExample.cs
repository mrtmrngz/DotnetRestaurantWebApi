using RestaurantApi.Application.Features.Roles.Commands;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.RoleExamples;

// Rol izin güncelleme isteğinin örnek gövdesi.
// RoleId route'tan geldiği için (JsonIgnore/BindNever) örnekte yalnızca gövde alanları bulunur;
// liste, rolün sahip olacağı izin kümesinin tamamıdır.
public class UpdateRolePermissionRequestExample: IExamplesProvider<UpdateRolePermissionCommand>
{
    public UpdateRolePermissionCommand GetExamples()
    {
        return new UpdateRolePermissionCommand(
            PermissionIds: new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            }
        );
    }
}
