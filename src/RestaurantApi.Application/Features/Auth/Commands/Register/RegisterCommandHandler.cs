using MediatR;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler: IRequestHandler<RegisterCommand, BaseResponse>
{

    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<BaseResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(request, cancellationToken);
    }
}