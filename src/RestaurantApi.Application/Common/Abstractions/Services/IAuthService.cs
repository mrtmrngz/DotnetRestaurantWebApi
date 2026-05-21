using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface IAuthService
{
    Task<BaseResponse> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
}