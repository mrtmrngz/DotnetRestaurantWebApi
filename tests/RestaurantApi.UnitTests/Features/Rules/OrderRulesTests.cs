using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.OrderRules;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.UnitTests.Features.Rules;

// Sipariş durum güncelleme akışının iş kuralları: her kural ya sessizce geçer ya da
// ilgili iş kuralı hatasını (404 / 403 / 409 / 422) doğru mesajla fırlatır.
public class OrderRulesTests
{
    private readonly ILogger<OrderRules> _loggerMock = Substitute.For<ILogger<OrderRules>>();
    private readonly OrderRules _sut;

    public OrderRulesTests()
    {
        _sut = new OrderRules(_loggerMock);
    }

    #region Helper Methods

    private static Domain.Entities.Order CreateOrder(Guid? userId, OrderStatus orderStatus = OrderStatus.Pending,
        PaymentStatus paymentStatus = PaymentStatus.Paid)
    {
        return new Domain.Entities.Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus
        };
    }

    #endregion

    #region ShouldOrderExist Tests

    [Fact]
    public void ShouldOrderExist_WhenOrderNotExist_ShouldThrowNotFoundException()
    {
        Action act = () => _sut.ShouldOrderExist(null, Guid.NewGuid());

        act.Should().Throw<NotFoundException>("Sipariş bulunamadığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Sipariş bulunamadı.");
    }

    [Fact]
    public void ShouldOrderExist_WhenOrderExist_ShouldNotThrow()
    {
        var order = CreateOrder(Guid.NewGuid());

        Action act = () => _sut.ShouldOrderExist(order, order.Id);

        act.Should().NotThrow("Sipariş bulunduğu için bu hata fırlatmaması gerekirdi");
    }

    #endregion

    #region ShouldOrderBelongToUser Tests

    [Fact]
    public void ShouldOrderBelongToUser_WhenOrderBelongsToUser_ShouldNotThrow()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId);

        Action act = () => _sut.ShouldOrderBelongToUser(order, userId);

        act.Should().NotThrow("Sipariş isteği yapan kullanıcıya ait olduğu için hata fırlatmaması gerekirdi");
    }

    [Fact]
    public void ShouldOrderBelongToUser_WhenOrderBelongsToAnotherUser_ShouldThrowForbiddenException()
    {
        var order = CreateOrder(Guid.NewGuid());
        var anotherUserId = Guid.NewGuid();

        Action act = () => _sut.ShouldOrderBelongToUser(order, anotherUserId);

        act.Should()
            .Throw<ForbiddenException>("Sipariş başka bir kullanıcıya ait olduğu için bu hata fırlatılması gerekirdi")
            .WithMessage("Bu sipariş size ait değil.");
    }

    [Fact]
    public void ShouldOrderBelongToUser_WhenOrderIsTableOrder_ShouldThrowForbiddenException()
    {
        // Masa siparişlerinde UserId boş olduğu için bu uçtan güncellenemezler.
        var tableOrder = CreateOrder(userId: null);

        Action act = () => _sut.ShouldOrderBelongToUser(tableOrder, Guid.NewGuid());

        act.Should()
            .Throw<ForbiddenException>("masa siparişi bir kullanıcıya ait olmadığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Bu sipariş size ait değil.");
    }

    #endregion

    #region ShouldOrderStatusBeDifferent Tests

    [Fact]
    public void ShouldOrderStatusBeDifferent_WhenStatusIsDifferent_ShouldNotThrow()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.Pending);

        Action act = () => _sut.ShouldOrderStatusBeDifferent(order.OrderStatus, OrderStatus.Confirmed, order.Id);

        act.Should().NotThrow("Yeni durum mevcut durumdan farklı olduğu için hata fırlatmaması gerekirdi");
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Canceled)]
    public void ShouldOrderStatusBeDifferent_WhenStatusIsSame_ShouldThrowConflictException(OrderStatus status)
    {
        var order = CreateOrder(Guid.NewGuid(), status);

        Action act = () => _sut.ShouldOrderStatusBeDifferent(order.OrderStatus, status, order.Id);

        act.Should()
            .Throw<ConflictException>("aynı duruma tekrar geçilemeyeceği için bu hata fırlatılması gerekirdi")
            .WithMessage($"Sipariş zaten {status} durumunda.");
    }

    #endregion

    #region ShouldOrderBePaid Tests

    [Fact]
    public void ShouldOrderBePaid_WhenPaymentStatusIsPaid_ShouldNotThrow()
    {
        var order = CreateOrder(Guid.NewGuid(), paymentStatus: PaymentStatus.Paid);

        Action act = () => _sut.ShouldOrderBePaid(order.PaymentStatus, order.Id);

        act.Should().NotThrow("Ödemesi alınmış siparişte hata fırlatmaması gerekirdi");
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    public void ShouldOrderBePaid_WhenPaymentIsNotCompleted_ShouldThrowUnprocessableEntityError(
        PaymentStatus paymentStatus)
    {
        var order = CreateOrder(Guid.NewGuid(), paymentStatus: paymentStatus);

        Action act = () => _sut.ShouldOrderBePaid(order.PaymentStatus, order.Id);

        act.Should()
            .Throw<UnprocessableEntityError>(
                "ödemesi tamamlanmayan siparişin durumu güncellenemeyeceği için bu hata fırlatılması gerekirdi")
            .WithMessage("Ödemesi tamamlanmayan siparişin durumu güncellenemez.");
    }

    #endregion

    #region ShouldOrderUpdateBeApplied Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void ShouldOrderUpdateBeApplied_WhenRowsAreAffected_ShouldNotThrow(int affectedRowCount)
    {
        Action act = () => _sut.ShouldOrderUpdateBeApplied(affectedRowCount, Guid.NewGuid());

        act.Should().NotThrow("Güncelleme uygulandığı için hata fırlatmaması gerekirdi");
    }

    [Fact]
    public void ShouldOrderUpdateBeApplied_WhenNoRowIsAffected_ShouldThrowConflictException()
    {
        // Eşzamanlı bir değişiklik: sipariş bu sırada güncellenmiş olabilir (ör. ödeme iptal edilmiş).
        Action act = () => _sut.ShouldOrderUpdateBeApplied(0, Guid.NewGuid());

        act.Should()
            .Throw<ConflictException>("hiçbir satır etkilenmediği için bu hata fırlatılması gerekirdi")
            .WithMessage("Sipariş durumu güncellenemedi. Lütfen tekrar deneyiniz.");
    }

    #endregion


}
