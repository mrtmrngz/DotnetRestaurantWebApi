using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;

// Tek bir kullanıcının bilgilerini getirme sorgusu (panel akışı).
// UserId controller tarafından route'tan set edilir; istemcinin query string/gövde ile başka bir
// kullanıcı seçmesini engellemek için [JsonIgnore] + [BindNever] ile korunur.
public record GetUserInfoQuery: IRequest<GeneralSuccessResponseWithData<GetUserInfoQueryResult>>
{
    [JsonIgnore]
    [BindNever]
    public Guid UserId { get; init; }
}
