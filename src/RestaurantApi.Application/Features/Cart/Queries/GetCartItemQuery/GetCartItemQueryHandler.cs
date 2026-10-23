using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;

public class GetCartItemQueryHandler : IRequestHandler<GetCartItemQuery,
    GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>>
{
    private readonly ILogger<GetCartItemQueryHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICartService _cartService;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IMapper _mapper;

    public GetCartItemQueryHandler(ILogger<GetCartItemQueryHandler> logger, ICacheService cacheService, ICartService cartService, ICartItemRepository cartItemRepository, IMapper mapper)
    {
        _logger = logger;
        _cacheService = cacheService;
        _cartService = cartService;
        _cartItemRepository = cartItemRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>> Handle(
        GetCartItemQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sepet detayı getiriliyor.");
        
        await _cartService.ValidateTargetEntityAsync(request.RequestType, request.UserId, request.RestaurantTableId,
            cancellationToken);
        
        var cacheKey = GetCacheKey(request.RequestType, request.UserId, request.RestaurantTableId);

        IReadOnlyList<GetCartQueryResult> items = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            Guid cartId = await _cartService.FindCartByRequest(request.RequestType, request.UserId,
                request.RestaurantTableId, cancellationToken);

            return await _cartItemRepository.GetAllAsQueryable()
                .AsNoTracking()
                .Where(x => x.CartId == cartId)
                .ProjectTo<GetCartQueryResult>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Sepetteki ürünler bulundu. Count:{Count}", items.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>(data: items);
    }

    #region Helper Methods

    private string GetCacheKey(CartRequestTypes requestTypes, Guid? userId, Guid? tableId)
    {
        if (requestTypes == CartRequestTypes.CurrentUser)
            return CacheKeys.UserCart(userId!.Value);

        return CacheKeys.PanelCart(tableId!.Value);
    }

    #endregion
}