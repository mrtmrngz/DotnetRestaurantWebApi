using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler: IRequestHandler<LoginCommand, LoginResponse>
{

    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly ISigninManager _signinManager;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;

    public LoginCommandHandler(UserRules userRules, IUserRepository userRepository, ILogger<LoginCommandHandler> logger, ISigninManager signinManager, IMediator mediator, IAuthService authService)
    {
        _userRules = userRules;
        _userRepository = userRepository;
        _logger = logger;
        _signinManager = signinManager;
        _mediator = mediator;
        _authService = authService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByEmailAsyncTracking(request.Email, cancellationToken);
        await _userRules.UserShouldExist401(user);

        // check lockout
        CheckIdentityLockout(user!);

        // password check
        await VerifyPasswordAsync(user!, request.Password);

        // check user verified status
        if (!await _userRules.ShouldUserVerified(user!))
        {
            return await HandleUnverifiedUserAsync(user!, request.Email, cancellationToken);
        }

        // 2fa check
        if (await _userRules.ShouldUserTwoFactorEnable(user!))
        {
            return await HandleTwoFactorAuthAsync(user!, request.Email);
        }

        // generate tokens
        return await _authService.HandleSuccessfulLoginAsync(user!, request.Email);
    }

    #region LoginHelperMethods

    private void CheckIdentityLockout(AppUser user)
    {
        if (user!.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            _logger.LogCritical("Kullanıcının hesabı kilitlendi. {Email}", user.Email);
            throw new ForbiddenException(
                "Çok fazla hatalı giriş denemesi yaptınız. Hesabınız geçici olarak kilitlendi.");
        }
    }
    
    private async Task VerifyPasswordAsync(AppUser user, string password)
    {
        var passwordCheckResult = await _signinManager.CheckPasswordSignInAsync(user!, password);

        if (passwordCheckResult.IsLockedOut)
        {
            _logger.LogCritical("Kullanıcının hesabı kilitlendi. {Email}", user.Email);
            throw new ForbiddenException(
                "Çok fazla hatalı giriş denemesi yaptınız. Hesabınız geçici olarak kilitlendi.");
        }

        if (!passwordCheckResult.Succeeded)
        {
            _logger.LogTrace("Kullanıcının şifresi yanlış. {Email}", user.Email);
            throw new UnauthorizedException("Geçersiz kimlik bilgileri.");
        }
    }
    
    private async Task<LoginResponse> HandleUnverifiedUserAsync(AppUser user, string email,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcı doğrulanmamış, mail adresine doğrulama bağlantısı gönderiliyor.");

        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user!);

        await _mediator.Publish(
            new UserRegisteredEvent(user!, token, email, $"{user!.Name} {user!.Surname}"),
            cancellationToken);

        return new LoginResponse()
        {
            Code = Codes.MAIL_VERIFICATION_REQUIRED,
            Message =
                "Giriş yapabilmek için ilk önce hesabınızı doğrulamalısınız, hesap doğrulama bağlantısı mail adresinize gönderildi."
        };
    }
    
    private async Task<LoginResponse> HandleTwoFactorAuthAsync(AppUser user, string email)
    {
        _logger.LogInformation(
            "Kullanıcının iki faktörlü kimlik doğrulaması açık, mail adresine 6 haneli kod gönderiliyor.");

        string otp = await _userRepository.GenerateTwoFactorTokenAsync(user!);

        await _mediator.Publish(new UserTwoFactorAuthEvent(user!, otp, email,
            $"{user!.Name} {user!.Surname}"));

        return new LoginResponse
        {
            Code = Codes.TWO_FACTOR_REQUIRED,
            Message = "Giriş yapabilmeniz için 6 haneli kod mail adresinize göndeirldi."
        };
    }

    #endregion
}