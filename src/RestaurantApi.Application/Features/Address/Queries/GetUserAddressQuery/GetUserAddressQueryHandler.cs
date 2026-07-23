using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;

public class GetUserAddressQueryHandler : IRequestHandler<GetUserAddressQuery,
    GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>>>
{
    private readonly IAddressRepository _addressRepository;
    private readonly ILogger<GetUserAddressQueryHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly UserRules _userRules;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserAddressQueryHandler(IAddressRepository addressRepository, ILogger<GetUserAddressQueryHandler> logger,
        ICacheService cacheService, UserRules userRules, IUserRepository userRepository, IMapper mapper)
    {
        _addressRepository = addressRepository;
        _logger = logger;
        _cacheService = cacheService;
        _userRules = userRules;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>>> Handle(
        GetUserAddressQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📍 [GetUserAddressQuery] Fetching address list for UserId: {UserId}", request.UserId);
        await _userRules.ShouldUserExistBool404(
            await _userRepository.AnyUserExistAsync(request.UserId, cancellationToken));

        var cacheKey = CacheKeys.UserAddressList(request.UserId.ToString());
        var addresses = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            _logger.LogInformation("🔄 [GetUserAddressQuery] Cache miss! Fetching address list from DB for UserId: {UserId}", request.UserId);
            
            var rawUserAddresses = await _addressRepository.GetUserAddressList(request.UserId, cancellationToken);
            
            _logger.LogInformation("📦 [GetUserAddressQuery] Retrieved {Count} addresses from DB for UserId: {UserId}", 
                rawUserAddresses.Count, request.UserId);

            return _mapper.Map<IReadOnlyList<GetUserAddressQueryResult>>(rawUserAddresses);
        }, TimeSpan.FromHours(1));
        
        _logger.LogInformation("✅ [GetUserAddressQuery] Successfully returned {Count} addresses for UserId: {UserId}", 
            addresses?.Count ?? 0, request.UserId);
        
        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>>(data: addresses!);
    }
}