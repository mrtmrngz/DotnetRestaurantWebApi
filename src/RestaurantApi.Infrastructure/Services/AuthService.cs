using Amazon.Runtime.Internal.Auth;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Common.Extensions;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Features.Files.Dtos.RefreshTokenDtos;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ISigninManager _signinManager;
    private readonly IGenerateRefreshToken _generateRefreshToken;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, UserRules userRules, IUnitOfWork uow,
        ILogger<AuthService> logger, IMapper mapper, IMediator mediator, ISigninManager signinManager, IGenerateRefreshToken generateRefreshToken, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _userRules = userRules;
        _uow = uow;
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _signinManager = signinManager;
        _generateRefreshToken = generateRefreshToken;
        _tokenService = tokenService;
    }
    
    public async Task<BaseResponse> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var existUser = await _userRepository.FindByEmailAsync(command.Email, cancellationToken);

        _logger.LogInformation("🔎 Aynı mail adresine sahip kullanıcı aranıyor: {email}", command.Email);

        await _userRules.ShouldUserNotExist(existUser);

        _logger.LogInformation("✅ Aynı mail adresine sahip kullanıcı bulunamadı {Email}", command.Email);

        var dbUser = _mapper.Map<AppUser>(command);
        dbUser.UserName = command.Email;

        await _uow.BeginTransaction(cancellationToken);

        var result = await _userRepository.CreateAsync(dbUser, command.Password);

        result.ThrowIfFailed();

        await _userRepository.AddToRoleAsync(dbUser, AppRoles.User);

        string token = await _userRepository.GenerateEmailConfirmationTokenAsync(dbUser);

        await _uow.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Kullanıcı oluşturuldu {Name} {Surname}", command.Name, command.Surname);

        await _mediator.Publish(
            new UserRegisteredEvent(dbUser, token, command.Email, $"{command.Name} {command.Surname}"),
            cancellationToken);

        return new BaseResponse
        {
            Message = "Kayıt işlemi başarılı, mail adresinize doğrulama bağlantısı gönderildi.",
            Code = Codes.CONTENT_CREATED_SUCCESS
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {

        var user = await _userRepository.FindByEmailAsync(command.Email, cancellationToken);

        await _userRules.UserShouldExist401(user);

        if (user!.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            _logger.LogCritical("Kullanıcının hesabı kilitlendi. {Email}", command.Email);
            throw new ForbiddenException(
                "Çok fazla hatalı giriş denemesi yaptınız. Hesabınız geçici olarak kilitlendi.");
        }
        
        // password check

        var passwordCheckResult = await _signinManager.CheckPasswordSignInAsync(user!, command.Password);

        if (passwordCheckResult.IsLockedOut)
        {
            _logger.LogCritical("Kullanıcının hesabı kilitlendi. {Email}", command.Email);
            throw new ForbiddenException(
                "Çok fazla hatalı giriş denemesi yaptınız. Hesabınız geçici olarak kilitlendi.");
        }

        if (!passwordCheckResult.Succeeded)
        {
            _logger.LogTrace("Kullanıcının şifresi yanlış. {Email}", command.Email);
            throw new UnauthorizedException("Geçersiz kimlik bilgileri.");
        }

        if (!await _userRules.ShouldUserVerified(user!))
        {
            _logger.LogInformation("Kullanıcı doğrulanmamış, mail adresine doğrulama bağlantısı gönderiliyor.");
            
            var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user!);
            
            await _mediator.Publish(
                new UserRegisteredEvent(user!, token, command.Email, $"{user!.Name} {user!.Surname}"),
                cancellationToken);
            
            return new LoginResponse()
            {
                Code = Codes.MAIL_VERIFICATION_REQUIRED,
                Message = "Giriş yapabilmek için ilk önce hesabınızı doğrulamalısınız, hesap doğrulama bağlantısı mail adresinize gönderildi."
            };
        }
        
        // 2fa check

        if (await _userRules.ShouldUserTwoFactorEnable(user!))
        {
            _logger.LogInformation("Kullanıcının iki faktörlü kimlik doğrulaması açık, mail adresine 6 haneli kod gönderiliyor.");
            
            string otp = await _userRepository.GenerateTwoFactorTokenAsync(user!);

            await _mediator.Publish(new UserTwoFactorAuthEvent(user!, otp, command.Email,
                $"{user!.Name} {user!.Surname}"));

            return new LoginResponse
            {
                Code = Codes.TWO_FACTOR_REQUIRED,
                Message = "Giriş yapabilmeniz için 6 haneli kod mail adresinize göndeirldi."
            };
        }
        
        // generate tokens

        var tokens = await GenerateAuthTokens(user!);
        
        _logger.LogInformation("Giriş başarılı: {email}", command.Email);

        return new LoginResponse()
        {
            Code = Codes.LOGIN_SUCCESS,
            Message = "Giriş başarılı.",
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
        };
    }

    private async Task<JwtAndRefreshTokenResult> GenerateAuthTokens(AppUser user)
    {
        _logger.LogInformation("Kullanıcı giriş yapabilir, tokenlar oluşturuluyor...");

        var refreshTokenTask = _generateRefreshToken.CreateRefreshToken(user.Id);

        var rolesTask = _userRepository.GetUserRolesAsync(user);

        await Task.WhenAll(refreshTokenTask, rolesTask);
        
        var refreshTokenResult = await refreshTokenTask;
        IList<string> roles = await rolesTask;

        // create access token

        var accessToken = await _tokenService.CreateAccessToken(user, roles);

        // return tokens
        
        _logger.LogInformation("Tokenlar oluşturuldu...");

        return new JwtAndRefreshTokenResult { AccessToken = accessToken, RefreshToken = refreshTokenResult.Token };
    }
}