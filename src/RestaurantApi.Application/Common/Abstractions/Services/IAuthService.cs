using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface IAuthService
{
    Task<BaseResponse> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);
}