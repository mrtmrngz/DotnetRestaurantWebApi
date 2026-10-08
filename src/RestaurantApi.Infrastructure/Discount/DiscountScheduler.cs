using Hangfire;
using RestaurantApi.Application.Common.Abstractions.Services;

namespace RestaurantApi.Infrastructure.Discount;

// Hangfire'ı kullanarak indirimlerin başlangıç/bitiş tarihlerinde çalışacak işleri planlar.
public class DiscountScheduler: IDiscountScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public DiscountScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void ScheduleActivation(Guid discountId, DateTime startDate)
    {
        _backgroundJobClient.Schedule<DiscountStatusJob>(
            job => job.ActivateAsync(discountId),
            new DateTimeOffset(DateTime.SpecifyKind(startDate, DateTimeKind.Utc)));
    }

    public void ScheduleDeactivation(Guid discountId, DateTime endDate)
    {
        _backgroundJobClient.Schedule<DiscountStatusJob>(
            job => job.DeactivateAsync(discountId),
            new DateTimeOffset(DateTime.SpecifyKind(endDate, DateTimeKind.Utc)));
    }
}
