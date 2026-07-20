using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.ProfileHandlers.Commands.ChangePasswordCommand;

public record ChangePasswordCommand(
    string OldPassword,
    string NewPassword
) : IRequest<BaseResponse>
{
    [JsonIgnore]
    public Guid UserId { get; init; }
};