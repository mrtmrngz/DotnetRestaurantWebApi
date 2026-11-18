using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;

// Bir kullanıcının rol durumunu listeleme sorgusu: tüm roller döner ve kullanıcının o role
// sahip olup olmadığı HasRole alanıyla bildirilir.
// UserId controller tarafından route'tan set edilir; istemcinin query string/gövde ile başka bir
// kullanıcı seçmesini engellemek için [JsonIgnore] + [BindNever] ile korunur.
public record GetUserRolesQuery: IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>>
{
    [JsonIgnore]
    [BindNever]
    public Guid UserId { get; init; }
}
