using MediatR;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler: IRequestHandler<LoginCommand, LoginResponse>
{

    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request, cancellationToken);
    }
}