using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Commands.UpdateUserRoleCommand;

public record UpdateUserRoleCommand(IReadOnlyCollection<string> RoleIds) : IRequest<BaseResponse>
{
    [JsonIgnore]
    [BindNever]
    public Guid UserId { get; init; }
};