using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.CartItemRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Cart.Commands.RemoveCartItemCommand;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, BaseResponse>
{
    private readonly ILogger<RemoveCartItemCommandHandler> _logger;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly CartItemRules _cartItemRules;
    private readonly ICartService _cartService;

    public RemoveCartItemCommandHandler(ILogger<RemoveCartItemCommandHandler> logger,
        ICartItemRepository cartItemRepository, CartItemRules cartItemRules, ICartService cartService)
    {
        _logger = logger;
        _cartItemRepository = cartItemRepository;
        _cartItemRules = cartItemRules;
        _cartService = cartService;
    }

    public async Task<BaseResponse> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sepetten ürün silme işlemleri başladı. RequestType:{RT}, ItemId:{II}",
            (request.RequestType), request.ItemId);

        // find user or restaurant table

        await _cartService.ValidateTargetEntityAsync(request.RequestType, request.UserId, request.RestaurantTableId,
            cancellationToken);

        // find cart

        Guid cartId = await _cartService.FindCartByRequest(request.RequestType, request.UserId,
            request.RestaurantTableId, cancellationToken);

        _logger.LogInformation("Sepetteki ürün miktarı güncelleniyor. CartId:{CI} ItemId:{CII}", cartId,
            request.ItemId);
        
        var affectedRows = await _cartItemRepository.GetAllAsQueryable()
            .Where(x => x.Id == request.ItemId && x.CartId == cartId)
            .ExecuteDeleteAsync(cancellationToken);
        
        _cartItemRules.ShouldCartItemAffected(affectedRows);
        
        _logger.LogInformation("Sepetteki ürün miktarı güncellendi.");

        await _cartService.InvalidateCartCache(request.RequestType, request.UserId, request.RestaurantTableId);

        _logger.LogInformation("Sepet ürün miktar güncellemesi tamamlandı.");

        return new BaseResponse
        {
            Code = Codes.CONTENT_DELETED_SUCCESS,
            Message = "Sepetteki ürün silindi."
        };
    }
}