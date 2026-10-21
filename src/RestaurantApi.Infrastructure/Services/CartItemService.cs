using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Features.Rules.CartItemRules;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Infrastructure.Services;

public class CartItemService: ICartItemService
{
    private readonly ILogger<CartItemService> _logger;
    private readonly CartItemRules _cartItemRules;

    public CartItemService(ILogger<CartItemService> logger, CartItemRules cartItemRules)
    {
        _logger = logger;
        _cartItemRules = cartItemRules;
    }

    public void IncreaseCartItemAsync(CartItem cartItem, int quantity, CancellationToken ctx)
    {
        _logger.LogInformation("Kart item miktar arttırma için, limitler kontrol ediliyor.");

        var newQuantity = checked(cartItem.Quantity + quantity);
        
        _cartItemRules.CheckMaximumCartValue(newQuantity);

        cartItem.Quantity = newQuantity;
    }
}