using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Security;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Infrastructure.Services;

public class AddressService : IAddressService
{
    private readonly ILogger<AddressService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly UserRules _userRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;

    public AddressService(ILogger<AddressService> logger, IUserRepository userRepository,
        IAddressRepository addressRepository, UserRules userRules, IUnitOfWork uow, ICacheService cacheService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _userRules = userRules;
        _uow = uow;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> CreateAddressAsyncService(Address dto, CancellationToken ctx)
    {
        _logger.LogInformation("Kullanıcı varlığı kontrol ediliyor. UserId: {UserId}", dto.UserId);
        
        await _userRules.ShouldUserExistBool404(
            await _userRepository.AnyUserExistAsync(dto.UserId, ctx)
        );

        _logger.LogInformation("Kullanıcının adres sayısı sorgulanıyor. UserId: {UserId}", dto.UserId);
        var userAddressCount = await _addressRepository.UserAddressCount(dto.UserId, ctx);

        await _uow.BeginTransaction(ctx);

        if (userAddressCount > 0 && dto.IsDefault)
        {
            _logger.LogInformation("Yeni adres varsayılan seçildiği için eski varsayılan adresler pasife çekiliyor. UserId: {UserId}", dto.UserId);
            await _addressRepository.UpdateOtherDefaultAddressToFalse(dto.UserId, ctx);
        }
        
        if (userAddressCount == 0)
        {
            _logger.LogInformation("Kullanıcının ilk adresi olduğu için otomatik varsayılan yapılıyor. UserId: {UserId}", dto.UserId);
            dto.IsDefault = true;
        }
        
        _addressRepository.CreateAddress(dto);

        await _uow.CommitTransactionAsync(ctx);
        
        _logger.LogInformation("Adres başarıyla oluşturuldu ve kaydedildi. AddressId: {AddressId}, UserId: {UserId}", dto.Id, dto.UserId);

        await _cacheService.RemoveAsync(CacheKeys.UserAddressList(dto.UserId.ToString()));

        return new BaseResponse
        {
            Code = Codes.CONTENT_CREATED_SUCCESS,
            Message = "Adres başarılı bir şekilde oluşturuldu."
        };
    }
}