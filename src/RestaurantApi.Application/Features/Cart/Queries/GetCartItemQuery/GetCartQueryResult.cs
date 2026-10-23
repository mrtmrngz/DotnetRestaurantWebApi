namespace RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;

public record GetCartQueryResult(
    string Title,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountRate,
    decimal SubTotal,
    decimal TotalPrice
);