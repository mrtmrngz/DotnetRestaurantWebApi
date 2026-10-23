namespace RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;

public class CartTotalProjection
{
    public decimal SubTotal { get; set; }
    public decimal AfterDiscountTotal { get; set; }
}