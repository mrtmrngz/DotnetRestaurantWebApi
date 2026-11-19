using RestaurantApi.Application.Features.Users.Commands.UpdateUserRoleCommand;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.UserExamples;

// Kullanıcı rol güncelleme isteğinin örnek gövdesi.
// UserId route'tan geldiği için (JsonIgnore/BindNever) örnekte yalnızca rol listesi bulunur.
public class UpdateUserRoleRequestExample: IExamplesProvider<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommand GetExamples()
    {
        return new UpdateUserRoleCommand(new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        });
    }
}
