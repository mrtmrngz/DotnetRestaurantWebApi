namespace RestaurantApi.Application.Common.Abstractions.Services;

// İndirimlerin başlangıç/bitiş tarihlerinde çalışacak zamanlanmış işleri planlar.
public interface IDiscountScheduler
{
    void ScheduleActivation(Guid discountId, DateTime startDate);
    void ScheduleDeactivation(Guid discountId, DateTime endDate);
}
