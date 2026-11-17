using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;

// Tek bir rolün bilgilerini getirme sorgusu (panel akışı).
// RoleId controller tarafından route'tan set edilir; istemcinin query string/gövde ile başka bir rol
// seçmesini engellemek için [JsonIgnore] + [BindNever] ile korunur.
public record GetRoleInfoQuery: IRequest<GeneralSuccessResponseWithData<GetRoleInfoQueryResult>>
{
    [JsonIgnore]
    [BindNever]
    public Guid RoleId { get; init; }
}
