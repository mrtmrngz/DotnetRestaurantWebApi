using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Roles.Commands;

// Rol bilgisini (adını) güncelleme komutu.
// Name istek gövdesinden gelir; RoleId ise route'tan controller tarafından set edilir,
// bu yüzden gövdeden okunmasını engellemek için [JsonIgnore] + [BindNever] ile korunur.
public sealed record UpdateRoleInfoCommand(string Name) : IRequest<BaseResponse>
{
    [JsonIgnore]
    [BindNever]
    public Guid RoleId { get; init; }
};
