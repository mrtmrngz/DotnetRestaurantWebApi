using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.AddressRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Dtos.AddressDtos;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Commands.RemoveAddressCommand;

public class RemoveAddressCommandHandler : IRequestHandler<RemoveAddressCommand, BaseResponse>
{
    private readonly ILogger<RemoveAddressCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly UserRules _userRules;
    private readonly AddressRules _addressRules;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveAddressCommandHandler(ILogger<RemoveAddressCommandHandler> logger, ICacheService cacheService,
        IUserRepository userRepository, IAddressRepository addressRepository, UserRules userRules,
        AddressRules addressRules, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _cacheService = cacheService;
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _userRules = userRules;
        _addressRules = addressRules;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(RemoveAddressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adres silme isteği başlatıldı. AddressId: {AddressId}, UserId: {UserId}",
            request.AddressId, request.UserId);

        var validationResult = await ValidateUserAndAddressAsync(request.UserId, request.AddressId, cancellationToken);

        bool wasDefault = validationResult.TargetAddress.IsDefault;

        await _unitOfWork.BeginTransaction(cancellationToken);

        validationResult.TargetAddress.IsDefault = false;
        validationResult.TargetAddress.IsDeleted = true;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (validationResult.AddressCount > 1 && wasDefault)
        {
            _logger.LogInformation(
                "Silinen adres varsayılan adresti. Diğer adreslerden en güncel olanı varsayılan olarak atanıyor. UserId: {UserId}",
                request.UserId);

            await _addressRepository.UpdateOtherLeastAddressToDefault(request.UserId, request.AddressId,
                cancellationToken);
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveAsync(CacheKeys.UserAddressList(request.UserId.ToString()));

        _logger.LogInformation(
            "Adres başarılı bir şekilde soft-delete yapıldı ve önbellek temizlendi. AddressId: {AddressId}, UserId: {UserId}, WasDefault: {WasDefault}",
            request.AddressId, request.UserId, wasDefault);

        return new BaseResponse
        {
            Code = Codes.CONTENT_DELETED_SUCCESS,
            Message = "Adres başarılı bir şekilde silindi."
        };
    }

    #region Remove Address Handler Helper Methods

    private async Task<(Domain.Entities.Address TargetAddress, int AddressCount)> ValidateUserAndAddressAsync(Guid userId, Guid addressId,
        CancellationToken ctx)
    {
        await _userRules.ShouldUserExistBool404(
            await _userRepository.AnyUserExistAsync(userId, ctx)
        );

        var targetAddress = await _addressRepository.FindUserActiveAddressByIdTracking(userId, addressId, ctx);

        await _addressRules.ShouldAddressExist(targetAddress, userId, addressId);

        var addressCount = await _addressRepository.UserAddressCount(userId, ctx);

        return (targetAddress!, addressCount);
    }

    #endregion
}