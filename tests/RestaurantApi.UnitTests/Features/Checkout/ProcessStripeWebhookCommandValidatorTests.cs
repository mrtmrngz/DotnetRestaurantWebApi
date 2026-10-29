using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;

namespace RestaurantApi.UnitTests.Features.Checkout;

// Webhook komutunun ŞEKİL doğrulaması: alanlar dolu mu, üst sınırlar aşılmış mı?
// İmzanın kriptografik doğrulaması (HMAC) bu validator'ın işi değildir; o PaymentService/handler tarafında test edilir.
public class ProcessStripeWebhookCommandValidatorTests
{
    // Validator'daki üst sınırlar (imza başlığı ve olay gövdesi).
    private const int MaxSignatureLength = 1024;
    private const int MaxPayloadLength = 1024 * 1024;

    private const string SignatureRequiredMessage = "Webhook imzası (Stripe-Signature) zorunludur.";
    private const string SignatureLengthMessage = "Webhook imzası en fazla 1024 karakter olabilir.";
    private const string PayloadRequiredMessage = "Webhook gövdesi zorunludur.";
    private const string PayloadLengthMessage = "Webhook gövdesi izin verilen boyutu aşıyor.";

    private readonly ProcessStripeWebhookCommandValidator _sut = new();

    #region Helper Methods

    private static ProcessStripeWebhookCommand CreateValidCommand()
    {
        return new ProcessStripeWebhookCommand
        {
            Signature = "t=1793000000,v1=5f3c9d2a1b0e4f6a8c7d9e0f1a2b3c4d",
            Payload = "{\"id\":\"evt_test\",\"type\":\"checkout.session.completed\"}"
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

    [Fact]
    public void Validate_WhenSignatureIsExactlyMaxLength_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand() with { Signature = new string('v', MaxSignatureLength) };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Signature);
    }

    [Fact]
    public void Validate_WhenPayloadIsExactlyMaxLength_ShouldNotHaveAnyValidationErrors()
    {
        // sınır değeri: 1 MB - 1 karakter değil, tam olarak 1 MB kabul edilmelidir
        var command = CreateValidCommand() with { Payload = new string('p', MaxPayloadLength) };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Payload);
    }

    [Fact]
    public void Validate_WhenPayloadIsNotJson_ShouldNotHaveValidationErrors()
    {
        // Validator yalnızca şekli doğrular: gövdenin çözümlenmesi (JSON) PaymentService'in işidir.
        var command = CreateValidCommand() with { Payload = "bu bir json değil" };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Signature Tests

    [Fact]
    public void Validate_WhenSignatureIsMissing_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { Signature = null! };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Signature)
            .WithErrorMessage(SignatureRequiredMessage);
    }

    [Fact]
    public void Validate_WhenSignatureIsEmpty_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { Signature = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Signature)
            .WithErrorMessage(SignatureRequiredMessage);
    }

    [Fact]
    public void Validate_WhenSignatureIsWhitespace_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { Signature = "   " };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Signature)
            .WithErrorMessage(SignatureRequiredMessage);
    }

    [Fact]
    public void Validate_WhenSignatureIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        var command = CreateValidCommand() with { Signature = new string('v', MaxSignatureLength + 1) };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Signature)
            .WithErrorMessage(SignatureLengthMessage);
    }

    [Fact]
    public void Validate_WhenSignatureIsMissing_ShouldNotAlsoAddLengthError()
    {
        // CascadeMode.Stop: ilk kural patlarsa uzunluk kuralı çalışmaz -> tek ve net mesaj döner
        var command = CreateValidCommand() with { Signature = string.Empty };

        var result = _sut.TestValidate(command);

        result.Errors.Should().HaveCount(1, "boş imza için yalnızca 'zorunludur' hatası dönmelidir.");
    }

    #endregion

    #region Payload Tests

    [Fact]
    public void Validate_WhenPayloadIsMissing_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { Payload = null! };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorMessage(PayloadRequiredMessage);
    }

    [Fact]
    public void Validate_WhenPayloadIsEmpty_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { Payload = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorMessage(PayloadRequiredMessage);
    }

    [Fact]
    public void Validate_WhenPayloadIsWhitespace_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { Payload = "  \t " };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorMessage(PayloadRequiredMessage);
    }

    [Fact]
    public void Validate_WhenPayloadIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        // DoS koruması: 1 MB üstü gövde, pahalı imza hesabına girmeden reddedilir
        var command = CreateValidCommand() with { Payload = new string('p', MaxPayloadLength + 1) };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorMessage(PayloadLengthMessage);
    }

    [Fact]
    public void Validate_WhenPayloadIsEmpty_ShouldNotAlsoAddLengthError()
    {
        var command = CreateValidCommand() with { Payload = string.Empty };

        var result = _sut.TestValidate(command);

        result.Errors.Should().HaveCount(1, "boş gövde için yalnızca 'zorunludur' hatası dönmelidir.");
    }

    #endregion

    #region Error Isolation and Multiple Error Tests

    [Fact]
    public void Validate_WhenOnlySignatureIsInvalid_ShouldNotHavePayloadValidationError()
    {
        var command = CreateValidCommand() with { Signature = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Payload);
        result.Errors.Should().HaveCount(1, "yalnızca imza hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyPayloadIsInvalid_ShouldNotHaveSignatureValidationError()
    {
        var command = CreateValidCommand() with { Payload = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Signature);
        result.Errors.Should().HaveCount(1, "yalnızca gövde hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenBothFieldsAreMissing_ShouldHaveBothValidationErrors()
    {
        // Controller'ın boş istek senaryosu: ne başlık ne gövde gelmiş
        var result = _sut.TestValidate(new ProcessStripeWebhookCommand());

        result.ShouldHaveValidationErrorFor(x => x.Signature)
            .WithErrorMessage(SignatureRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorMessage(PayloadRequiredMessage);

        result.Errors.Should().HaveCount(2, "iki alan da boş olduğu için iki hata dönmelidir.");
    }

    [Fact]
    public void Validate_WhenSignatureIsTooLongAndPayloadIsMissing_ShouldHaveBothValidationErrors()
    {
        var command = new ProcessStripeWebhookCommand
        {
            Signature = new string('v', MaxSignatureLength + 10),
            Payload = string.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Signature)
            .WithErrorMessage(SignatureLengthMessage);
        result.ShouldHaveValidationErrorFor(x => x.Payload)
            .WithErrorMessage(PayloadRequiredMessage);
    }

    #endregion
}
