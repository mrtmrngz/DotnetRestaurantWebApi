using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using NSubstitute;
using RestaurantApi.Infrastructure.Discount;

namespace RestaurantApi.UnitTests.Features.Discount;

public class DiscountSchedulerTests
{
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly DiscountScheduler _sut;

    public DiscountSchedulerTests()
    {
        _sut = new DiscountScheduler(_backgroundJobClient);
    }

    [Fact]
    public void ScheduleActivation_ShouldScheduleActivateJobWithDiscountId()
    {
        var discountId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(1);

        _sut.ScheduleActivation(discountId, startDate);

        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(job =>
                job.Method.Name == nameof(DiscountStatusJob.ActivateAsync) &&
                (Guid)job.Args[0] == discountId),
            Arg.Is<IState>(state => state is ScheduledState));
    }

    [Fact]
    public void ScheduleDeactivation_ShouldScheduleDeactivateJobWithDiscountId()
    {
        var discountId = Guid.NewGuid();
        var endDate = DateTime.UtcNow.AddDays(3);

        _sut.ScheduleDeactivation(discountId, endDate);

        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(job =>
                job.Method.Name == nameof(DiscountStatusJob.DeactivateAsync) &&
                (Guid)job.Args[0] == discountId),
            Arg.Is<IState>(state => state is ScheduledState));
    }
}
