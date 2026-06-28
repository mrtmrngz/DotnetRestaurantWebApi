using System.Text;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Commands.MailVerify;

public class MailVerifyCommandHandler: IRequestHandler<MailVerifyCommand, BaseResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<MailVerifyCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IMediator _mediator;

    public MailVerifyCommandHandler(ICacheService cacheService, ILogger<MailVerifyCommandHandler> logger, IUserRepository userRepository, UserRules userRules, IMediator mediator)
    {
        _cacheService = cacheService;
        _logger = logger;
        _userRepository = userRepository;
        _userRules = userRules;
        _mediator = mediator;
    }

    public async Task<BaseResponse> Handle(MailVerifyCommand request, CancellationToken cancellationToken)
    {
        // find and validate user
        var user = await FindAndValidateUser(request.Token);

        await _userRules.ShouldUserNotVerified(user);

        // decoded token
        var decodedToken = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(request.Token)
        );

        // verify user
        await HandleVerifyUser(user, decodedToken);

        // verified event
        await _mediator.Publish(new MailVerifiedEvent(user, request.Token));

        return new BaseResponse
        {
            Code = Codes.MAIL_VERIFIED_SUCCESS,
            Message = "Mail adresiniz başarıyla doğrulandı, giriş yapabilirsiniz."
        };
    }

    #region MailVerifyHelpers

    private async Task<AppUser> FindAndValidateUser(string token)
    {
        var userId = await _cacheService.GetAsync<string>(CacheKeys.MailVerificationToken(token));

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("Kullanıcnın token süresi geçmiş veya geçersiz token girmiş: {token}",
                token);
            throw new ForbiddenException("Geçersiz token");
        }

        var user = await _userRepository.GetByIdTrackingAsync(Guid.Parse(userId));

        await _userRules.UserShouldExist404(user);

        return user!;
    }

    private async Task HandleVerifyUser(AppUser user, string decodedToken)
    {
        await _userRules.ShouldUserVerifiedSucceded(await _userRepository.ConfirmEmailAsync(user, decodedToken));

        await _userRepository.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Kullanıcının mail adresi başarıyla dorğulandı: {Email}", user.Email);
    }

    #endregion
}