namespace RestaurantApi.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Ready = 3,
    OnTheWay = 4,
    Delivered = 5,
    Canceled = 6
}