using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;

public class ForgotPasswordVerifyCommandHandler: IRequestHandler<ForgotPasswordVerifyCommand, BaseResponse>
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ForgotPasswordVerifyCommandHandler> _logger;

    public ForgotPasswordVerifyCommandHandler(IMediator mediator, IUserRepository userRepository, ILogger<ForgotPasswordVerifyCommandHandler> logger)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(ForgotPasswordVerifyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mail gönderilmek ve token oluşturulmak için kullanıcı aranıyor: {Email}", request.Email);
        var user = await _userRepository.FindByEmailAsyncTracking(request.Email, cancellationToken);

        if (user is not null && !user.IsDeleted)
        {
            _logger.LogInformation("Kullanıcı bulundu token oluşturuluyor: {Email}", request.Email);
            var rawToken = await _userRepository.GeneratePasswordResetTokenAsync(user, cancellationToken);
            await _mediator.Publish(new ForgotPasswordVerifyEvent(user.Id.ToString(), rawToken, request.Email,
                $"{user.Name} {user.Surname}", 15));
        }
        
        return new BaseResponse
        {
            Message = "Kimliğinizi doğrulayabilmek için email adresinize bağlantı gönderildi",
            Code = Codes.MAIL_SENT_SUCCESS
        };
    }
}