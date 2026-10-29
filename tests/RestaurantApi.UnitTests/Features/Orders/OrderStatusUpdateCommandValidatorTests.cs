using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.UnitTests.Features.Orders;

// Sipariş durum güncelleme komutunun şekil doğrulaması: tüm alanlar zorunludur ve
// yeni durum geçerli bir OrderStatus değeri olmalıdır.
public class OrderStatusUpdateCommandValidatorTests
{
    private const string OrderIdRequiredMessage = "Sipariş ID bilgisi zorunludur.";
    private const string UserIdRequiredMessage = "Kullanıcı ID bilgisi zorunludur.";
    private const string NewStatusRequiredMessage = "Sipariş durumu zorunludur.";
    private const string NewStatusInvalidMessage = "Geçersiz sipariş durumu.";

    private readonly OrderStatusUpdateCommandValidator _sut = new();

    #region Helper Methods

    private static OrderStatusUpdateCommand CreateValidCommand()
    {
        return new OrderStatusUpdateCommand(UserId: Guid.NewGuid(), NewStatus: OrderStatus.Confirmed)
        {
            OrderId = Guid.NewGuid()
        };
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.OnTheWay)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Canceled)]
    public void Validate_WhenNewStatusIsAValidTargetStatus_ShouldNotHaveNewStatusValidationError(
        OrderStatus newStatus)
    {
        var command = CreateValidCommand() with { NewStatus = newStatus };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.NewStatus);
    }

    #endregion

    #region Field Required Tests

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldHaveOrderIdRequiredError()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage(OrderIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldHaveUserIdRequiredError()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenNewStatusIsPending_ShouldHaveNewStatusRequiredError()
    {
        // Pending (0) geçerli bir hedef durum değildir: güncelleme isteniyorsa açıkça bir durum gönderilmelidir.
        var command = CreateValidCommand() with { NewStatus = OrderStatus.Pending };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.NewStatus)
            .WithErrorMessage(NewStatusRequiredMessage);
    }

    [Fact]
    public void Validate_WhenNewStatusIsOutOfEnumRange_ShouldHaveInvalidStatusError()
    {
        var command = CreateValidCommand() with { NewStatus = (OrderStatus)99 };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.NewStatus)
            .WithErrorMessage(NewStatusInvalidMessage);
    }

    [Fact]
    public void Validate_WhenNewStatusIsPending_ShouldNotAlsoAddInvalidStatusError()
    {
        // CascadeMode.Stop: "zorunludur" hatasından sonra enum kontrolü çalışmaz -> tek ve net mesaj
        var command = CreateValidCommand() with { NewStatus = OrderStatus.Pending };

        var result = _sut.TestValidate(command);

        result.Errors.Should().HaveCount(1, "boş durum için yalnızca 'zorunludur' hatası dönmelidir.");
    }

    #endregion

    #region Error Isolation and Multiple Error Tests

    [Fact]
    public void Validate_WhenOnlyOrderIdIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.ShouldNotHaveValidationErrorFor(x => x.NewStatus);
        result.Errors.Should().HaveCount(1, "yalnızca sipariş ID hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyUserIdIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
        result.ShouldNotHaveValidationErrorFor(x => x.NewStatus);
        result.Errors.Should().HaveCount(1, "yalnızca kullanıcı ID hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyNewStatusIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = CreateValidCommand() with { NewStatus = OrderStatus.Pending };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.Errors.Should().HaveCount(1, "yalnızca sipariş durumu hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenAllFieldsAreMissing_ShouldHaveThreeValidationErrors()
    {
        // Boş gövde + route'tan sipariş id gelmemesi senaryosu: üç alan da boş kalır.
        var result = _sut.TestValidate(new OrderStatusUpdateCommand(UserId: Guid.Empty,
            NewStatus: OrderStatus.Pending));

        result.ShouldHaveValidationErrorFor(x => x.OrderId).WithErrorMessage(OrderIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.UserId).WithErrorMessage(UserIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.NewStatus).WithErrorMessage(NewStatusRequiredMessage);

        result.Errors.Should().HaveCount(3, "üç alan da geçersiz olduğu için üç hata dönmelidir.");
    }

    #endregion
}
