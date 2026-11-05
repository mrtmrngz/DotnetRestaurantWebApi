using RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetRolePermissionsResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>> GetExamples()
    {
        var mockData = new List<GetRolePermissionsQueryResult>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "orders:read",
                Description = "Yetki sahibi kullanıcılar siparişleri listeleyebilir."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "role:update",
                Description = "Role güncelleme yetkisi verir."
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "table:view",
                Description = "Masaları görüntüleme yetkisi verir."
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>(data: mockData);
    }
}
