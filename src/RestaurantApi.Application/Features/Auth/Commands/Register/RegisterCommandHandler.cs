using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Extensions;
using RestaurantApi.Application.Features.Auth.Events;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, BaseResponse>
{
    private readonly ILogger<RegisterCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _uow;

    public RegisterCommandHandler(ILogger<RegisterCommandHandler> logger, IUserRepository userRepository,
        UserRules userRules, IMediator mediator, IMapper mapper, IUnitOfWork uow)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userRules = userRules;
        _mediator = mediator;
        _mapper = mapper;
        _uow = uow;
    }

    public async Task<BaseResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // find exist user and validate
        await FindExistUserAndValidate(request.Email, cancellationToken);

        var dbUser = _mapper.Map<AppUser>(request);
        dbUser.UserName = request.Email;

        await _uow.BeginTransaction(cancellationToken);

        var result = await _userRepository.CreateAsync(dbUser, request.Password);

        result.ThrowIfFailed();

        await _userRepository.AddToRoleAsync(dbUser, AppRoles.User);

        string token = await _userRepository.GenerateEmailConfirmationTokenAsync(dbUser);

        await _uow.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Kullanıcı oluşturuldu {Name} {Surname}", request.Name, request.Surname);

        await _mediator.Publish(
            new UserRegisteredEvent(dbUser, token, request.Email, $"{request.Name} {request.Surname}"),
            cancellationToken);

        return new BaseResponse
        {
            Message = "Kayıt işlemi başarılı, mail adresinize doğrulama bağlantısı gönderildi.",
            Code = Codes.CONTENT_CREATED_SUCCESS
        };
    }

    #region RegisterHelperMethods

    private async Task FindExistUserAndValidate(string email, CancellationToken cancellationToken)
    {
        var existUser = await _userRepository.FindByEmailAsync(email, cancellationToken);

        _logger.LogInformation("🔎 Aynı mail adresine sahip kullanıcı aranıyor: {email}", email);

        await _userRules.ShouldUserNotExist(existUser);

        _logger.LogInformation("✅ Aynı mail adresine sahip kullanıcı bulunamadı {Email}", email);
    }

    #endregion
}