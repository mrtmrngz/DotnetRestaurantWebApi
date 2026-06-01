using MediatR;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.MailVerify;

public class MailVerifyCommandHandler: IRequestHandler<MailVerifyCommand, BaseResponse>
{
    private readonly IAuthService _authService;

    public MailVerifyCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<BaseResponse> Handle(MailVerifyCommand request, CancellationToken cancellationToken)
    {
        return await _authService.MailVerifyAsync(request, cancellationToken);
    }
}