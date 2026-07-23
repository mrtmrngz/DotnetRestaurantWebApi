using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;
using RestaurantApi.Application.Features.Rules.AddressRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Queries.GetUserAddressByIdQuery;

public class GetUserAddressByIdQueryHandler
    : IRequestHandler<GetUserAddressByIdQuery, GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>>
{
    private readonly ICacheService _cacheService;
    private readonly IAddressRepository _addressRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly AddressRules _addressRules;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserAddressByIdQueryHandler> _logger;

    public GetUserAddressByIdQueryHandler(ICacheService cacheService, IAddressRepository addressRepository, IUserRepository userRepository, UserRules userRules, AddressRules addressRules, IMapper mapper, ILogger<GetUserAddressByIdQueryHandler> logger)
    {
        _cacheService = cacheService;
        _addressRepository = addressRepository;
        _userRepository = userRepository;
        _userRules = userRules;
        _addressRules = addressRules;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>> Handle(
        GetUserAddressByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Adres detayı getirilme isteği başlatıldı. UserId: {UserId}, AddressId: {AddressId}", 
            request.UserId, request.AddressId);
        
        await _userRules.ShouldUserExistBool404(
            await _userRepository.AnyUserExistAsync(request.UserId, cancellationToken));

        var cacheData = await CacheActions(request.UserId, request.AddressId);

        if (cacheData is not null)
        {
            return new GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>(cacheData);
        }

        var addressInDb = await DatabaseActions(request.UserId, request.AddressId, cancellationToken);
        
        return new GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>(addressInDb);
    }

    private async Task<GetUserAddressByIdQueryResult?> CacheActions(Guid userId, Guid addressId)
    {
        var cacheKey = CacheKeys.UserAddressList(userId.ToString());

        var addressesInCache = await _cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(cacheKey);

        if (addressesInCache is not null)
        {
            _logger.LogInformation(
                "Adres listesi Redis cache'ten okundu. CacheKey: {CacheKey}, UserId: {UserId}", 
                cacheKey, userId);
            
            var addressInCache = addressesInCache.FirstOrDefault(add => add.Id == addressId);

            await _addressRules.ShouldAddressExistInCache(addressInCache, userId, addressId);

            var cachedDto = _mapper.Map<GetUserAddressByIdQueryResult>(addressInCache);
            
            _logger.LogInformation(
                "Adres detayı Redis cache üzerinden başarıyla dönüldü. AddressId: {AddressId}, UserId: {UserId}", 
                addressId, userId);
        
            return cachedDto;
        }

        return null;
    }

    private async Task<GetUserAddressByIdQueryResult> DatabaseActions(Guid userId, Guid addressId, CancellationToken ctx)
    {
        _logger.LogInformation(
            "Adres listesi cache'te bulunamadı (Cache Miss). Veritabanına sorgu atılıyor. UserId: {UserId}, AddressId: {AddressId}", 
            userId, addressId);

        var rawAddressInDb =
            await _addressRepository.FindUserActiveAddress(userId, addressId, ctx);

        await _addressRules.ShouldAddressExist(rawAddressInDb, userId, addressId);

        var addressInDb = _mapper.Map<GetUserAddressByIdQueryResult>(rawAddressInDb);
        
        _logger.LogInformation(
            "Adres detayı veritabanından başarıyla getirildi. AddressId: {AddressId}, UserId: {UserId}", 
            addressId, userId);

        return addressInDb;
    }
}