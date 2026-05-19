using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Extensions;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Features.Auth.Events;
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

    public AuthService(IUserRepository userRepository, UserRules userRules, IUnitOfWork uow,
        ILogger<AuthService> logger, IMapper mapper, IMediator mediator)
    {
        _userRepository = userRepository;
        _userRules = userRules;
        _uow = uow;
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
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
}