using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.AddressRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Commands.UpdateAddressCommand;

public class AddressValidateDto
{
    public Domain.Entities.Address Address { get; set; } = null!;
    public int Count { get; set; }
}

public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, BaseResponse>
{
    private readonly ILogger<UpdateAddressCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly AddressRules _addressRules;
    private readonly UserRules _userRules;

    public UpdateAddressCommandHandler(ILogger<UpdateAddressCommandHandler> logger, IUnitOfWork unitOfWork,
        ICacheService cacheService, IUserRepository userRepository, IAddressRepository addressRepository,
        AddressRules addressRules, UserRules userRules)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _addressRules = addressRules;
        _userRules = userRules;
    }

    public async Task<BaseResponse> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting address update process for User {UserId}, Address {AddressId}",
            request.UserId, request.AddressId);

        var validateData = await ValidateAddress(request, cancellationToken);

        bool wasDefault = validateData.Address.IsDefault;

        await _unitOfWork.BeginTransaction(cancellationToken);

        UpdateAddressEntityValues(validateData.Address, request);

        await ManageDefaultAddressStateAsync(
            request, 
            wasDefault, 
            validateData.Count, 
            cancellationToken);

        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation(
            "Address {AddressId} successfully updated in database for User {UserId}",
            request.AddressId, request.UserId);

        var cacheKey = CacheKeys.UserAddressList(request.UserId.ToString());

        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogDebug("Invalidated address list cache for key: {CacheKey}", cacheKey);

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = "Adres başarılı bir şekilde güncellendi."
        };
    }


    #region Update Address Helper Methods

    private async Task<AddressValidateDto> ValidateAddress(UpdateAddressCommand request, CancellationToken ctx)
    {
        await _userRules.ShouldUserExistBool404(
            await _userRepository.AnyUserExistAsync(request.UserId, ctx)
        );

        var targetAddress =
            await _addressRepository.FindUserActiveAddressByIdTracking(request.UserId, request.AddressId,
                ctx);

        await _addressRules.ShouldAddressExist(targetAddress, request.UserId, request.AddressId);

        var addressCount = await _addressRepository.UserAddressCount(request.UserId, ctx);

        await _addressRules.ShouldUserHasAnotherAddress(
            addressCount, request.IsDefault, targetAddress!.IsDefault, request.UserId, request.AddressId
        );

        return new AddressValidateDto { Address = targetAddress, Count = addressCount };
    }

    private async Task ManageDefaultAddressStateAsync(
        UpdateAddressCommand request,
        bool wasDefault,
        int totalAddressCount,
        CancellationToken cancellationToken)
    {
        if (totalAddressCount <= 1) return;
        
        if (request.IsDefault is true && !wasDefault)
        {
            _logger.LogInformation(
                "Setting Address {AddressId} as default and unsetting other default addresses for User {UserId}",
                request.AddressId, request.UserId);

            await _addressRepository.UpdateOtherDefaultAddressToFalse(request.UserId, cancellationToken);
        }

        if (request.IsDefault is false && wasDefault)
        {
            _logger.LogInformation(
                "Address {AddressId} unset from default for User {UserId}. Promoting latest address to default.",
                request.AddressId, request.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _addressRepository.UpdateOtherLeastAddressToDefault(request.UserId, request.AddressId,
                cancellationToken);
        }
    }

    private static void UpdateAddressEntityValues(Domain.Entities.Address address, UpdateAddressCommand request)
    {
        address.Title = request.Title ?? address.Title;
        address.RecipientName = request.RecipientName ?? address.RecipientName;
        address.City = request.City ?? address.City;
        address.Town = request.Town ?? address.Town;
        address.Neighborhood = request.Neighborhood ?? address.Neighborhood;
        address.Street = request.Street ?? address.Street;
        address.BuildingInfo = request.BuildingInfo ?? address.BuildingInfo;
        address.BuildingNumber = request.BuildingNumber ?? address.BuildingNumber;
        address.PhoneNumber = request.PhoneNumber ?? address.PhoneNumber;
        address.ZipCode = request.ZipCode ?? address.ZipCode;
        address.IsDefault = request.IsDefault ?? address.IsDefault;
    }

    #endregion
}