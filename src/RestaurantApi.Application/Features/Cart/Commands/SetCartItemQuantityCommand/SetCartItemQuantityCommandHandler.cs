using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.CartItemRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Cart.Commands.SetCartItemQuantityCommand;

public class SetCartItemQuantityCommandHandler : IRequestHandler<SetCartItemQuantityCommand, BaseResponse>
{
    private readonly ILogger<SetCartItemQuantityCommandHandler> _logger;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly CartItemRules _cartItemRules;
    private readonly ICartService _cartService;

    public SetCartItemQuantityCommandHandler(ILogger<SetCartItemQuantityCommandHandler> logger,
        ICartItemRepository cartItemRepository,
        CartItemRules cartItemRules, ICartService cartService)
    {
        _logger = logger;
        _cartItemRepository = cartItemRepository;
        _cartItemRules = cartItemRules;
        _cartService = cartService;
    }

    public async Task<BaseResponse> Handle(SetCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sepet ürün miktarı güncelleme işlemleri başladı. RequestEntityId:{EntityId} Quantity:{Q}, ItemId:{II}",
            (request.RestaurantTableId ?? request.UserId), request.Quantity, request.ItemId);

        // find user or restaurant table

        await _cartService.ValidateTargetEntityAsync(request.RequestType, request.UserId, request.RestaurantTableId,
            cancellationToken);

        // find cart

        Guid cartId = await _cartService.FindCartByRequest(request.RequestType, request.UserId,
            request.RestaurantTableId, cancellationToken);
        
        _logger.LogInformation("Sepetteki ürün miktarı güncelleniyor.");

        var affectedRows = await _cartItemRepository.GetAllAsQueryable()
            .Where(x => x.Id == request.ItemId && x.CartId == cartId)
            .ExecuteUpdateAsync(setter =>
                setter.SetProperty(ci => ci.Quantity, request.Quantity), cancellationToken);
        
        _cartItemRules.ShouldCartItemAffected(affectedRows);
        
        _logger.LogInformation("Sepetteki ürün silindi.");

        await _cartService.InvalidateCartCache(request.RequestType, request.UserId, request.RestaurantTableId);

        _logger.LogInformation("Sepet ürün silme işlemleri tamamlandı.");

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = "Sepetteki ürün başarılı bir şekilde silindi."
        };
    }
}