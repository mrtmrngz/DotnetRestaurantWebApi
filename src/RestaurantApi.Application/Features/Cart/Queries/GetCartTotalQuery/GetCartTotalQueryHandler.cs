using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;

public class
    GetCartTotalQueryHandler : IRequestHandler<GetCartTotalQuery,
    GeneralSuccessResponseWithData<GetCartTotalQueryResult>>
{
    private readonly ILogger<GetCartTotalQueryHandler> _logger;
    private readonly ICartService _cartService;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IMapper _mapper;

    public GetCartTotalQueryHandler(ILogger<GetCartTotalQueryHandler> logger, ICartService cartService,
        ICartItemRepository cartItemRepository, IMapper mapper)
    {
        _logger = logger;
        _cartService = cartService;
        _cartItemRepository = cartItemRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetCartTotalQueryResult>> Handle(GetCartTotalQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sepet tutarı hesaplanıyor...");

        await _cartService.ValidateTargetEntityAsync(request.RequestType, request.UserId, request.RestaurantTableId,
            cancellationToken);

        GetCartTotalQueryResult result = await CalculateTotal(request.RequestType, request.UserId, request.RestaurantTableId, cancellationToken);
        
        _logger.LogInformation("Sepet tutarı hesaplandı...");

        return new GeneralSuccessResponseWithData<GetCartTotalQueryResult>(data: result);
    }

    #region Calculate Total

    private async Task<GetCartTotalQueryResult> CalculateTotal(CartRequestTypes requestType, Guid? userId, Guid? tableId, CancellationToken ctx)
    {
        GetCartTotalQueryResult nullData = new GetCartTotalQueryResult(Subtotal: 0m, AfterDiscountTotal: 0m,
            SavingTotal: 0m, ShippingCost: 0m, Total: 0m);
        
        Domain.Entities.Cart? cart = await _cartService.FindCartWithoutValidate(requestType, userId, tableId, ctx);

        if (cart == null)
            return nullData;
        
        var result = await _cartItemRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(x => x.CartId == cart.Id)
            .GroupBy(x => x.CartId)
            .Select(g => new CartTotalProjection
            {
                SubTotal = g.Sum(x => x.Product.Price * x.Quantity),
                AfterDiscountTotal = g.Sum(x =>
                    x.Product.Price * x.Quantity *
                    (1m - (
                        x.Product.Discounts
                            .Where(d =>
                                d.IsActive &&
                                d.StartDate <= DateTime.UtcNow &&
                                d.EndDate >= DateTime.UtcNow)
                            .Select(d => (decimal?)d.Rate)
                            .FirstOrDefault() ?? 0m
                    ) / 100m))
            })
            .ProjectTo<GetCartTotalQueryResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ctx);

        return result ?? nullData;
    }

    #endregion
}