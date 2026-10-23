namespace RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;

public record GetCartTotalQueryResult(
    decimal Subtotal,
    decimal AfterDiscountTotal,
    decimal SavingTotal,
    decimal ShippingCost,
    decimal Total
);