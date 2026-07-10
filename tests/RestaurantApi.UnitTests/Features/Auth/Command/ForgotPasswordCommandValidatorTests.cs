using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;

namespace RestaurantApi.UnitTests.Features.Auth.Command;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator;

    public ForgotPasswordCommandValidatorTests()
    {
        _validator = new ForgotPasswordCommandValidator();
    }
    
    // ERROR TESTS START
    [Fact]
    public async Task Email_WhenIsEmpty_ShouldHaveValidationError()
    {
        var command = new ForgotPasswordVerifyCommand(Email: "");

        // Act & Assert
        var result = _validator.TestValidate(command);
        
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email alanı boş olamaz.");
    }
    
    [Theory]
    [InlineData("invalidemail")]
    [InlineData("invalidemail.com")]
    public async Task Email_WhenIsInvalidEmail_ShouldHaveValidationError(string invalidEmail)
    {
        var command = new ForgotPasswordVerifyCommand(Email: invalidEmail);

        // Act & Assert
        var result = _validator.TestValidate(command);
        
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Geçersiz email adresi.");
    }
    
    // ERROR TESTS END
    
    // SUCCESS TESTS START
    
    [Theory]
    [InlineData("test@gmail.com")]
    [InlineData("test+test+test_test@gmail.com")]
    public async Task Email_WhenIsValid_ShouldNotHaveValidationError(string validEmail)
    {
        var command = new ForgotPasswordVerifyCommand(Email: validEmail);

        var result = _validator.TestValidate(command);
        
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
    // SUCCESS TESTS END
}