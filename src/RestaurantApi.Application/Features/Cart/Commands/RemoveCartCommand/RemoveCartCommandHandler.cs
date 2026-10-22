using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.CartItemRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Cart.Commands.RemoveCartCommand;

public class RemoveCartCommandHandler: IRequestHandler<RemoveCartCommand, BaseResponse>
{
    private readonly ILogger<RemoveCartCommandHandler> _logger;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly ICartService _cartService;

    public RemoveCartCommandHandler(ILogger<RemoveCartCommandHandler> logger, ICartItemRepository cartItemRepository, ICartService cartService)
    {
        _logger = logger;
        _cartItemRepository = cartItemRepository;
        _cartService = cartService;
    }

    public async Task<BaseResponse> Handle(RemoveCartCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sepet temizleme işlemleri başladı. RequestType:{RT}",
            (request.RequestType));

        // find user or restaurant table

        await _cartService.ValidateTargetEntityAsync(request.RequestType, request.UserId, request.RestaurantTableId,
            cancellationToken);

        // find cart

        Guid cartId = await _cartService.FindCartByRequest(request.RequestType, request.UserId,
            request.RestaurantTableId, cancellationToken);

        _logger.LogInformation("Sepet temizleme işlemi başlıyor... CartId:{CI}", cartId);
        
        await _cartItemRepository.GetAllAsQueryable()
            .Where(x => x.CartId == cartId)
            .ExecuteDeleteAsync(cancellationToken);
        
        _logger.LogInformation("Sepet temizlendi.");

        await _cartService.InvalidateCartCache(request.RequestType, request.UserId, request.RestaurantTableId);

        _logger.LogInformation("Sepet temizleme işlemleri tamamlandı.");

        return new BaseResponse
        {
            Code = Codes.CONTENT_DELETED_SUCCESS,
            Message = "Sepet temizlendi."
        };
    }
}