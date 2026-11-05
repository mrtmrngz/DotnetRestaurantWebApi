using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Roles.Commands;

public sealed record UpdateRolePermissionCommand(IReadOnlyCollection<Guid> PermissionIds) : IRequest<BaseResponse>
{
    [JsonIgnore]
    [BindNever]
    public Guid RoleId { get; init; }
};
