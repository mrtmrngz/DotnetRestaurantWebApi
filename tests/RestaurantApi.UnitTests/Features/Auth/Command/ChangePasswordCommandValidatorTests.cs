using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.ProfileHandlers.Commands.ChangePasswordCommand;

namespace RestaurantApi.UnitTests.Features.Auth.Command;

public class ChangePasswordCommandValidatorTests
{

    private readonly ChangePasswordCommandValidator _validator;

    public ChangePasswordCommandValidatorTests()
    {
        _validator = new ChangePasswordCommandValidator();
    }

    // SUCCESS TEST START

    [Theory]
    [InlineData("Test123", "Test1234")]
    [InlineData("Test1233535##ff", "Test1234#$#½")]
    public async Task ChangePasswordCommandValidator_WhenValidData_ShouldNotHaveValidationError(string oldPassword, string newPassword)
    {
        var command = new ChangePasswordCommand(oldPassword, newPassword);

        var result = _validator.TestValidate(command);
        
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START

    [Theory]
    [InlineData("", "Test123")]
    [InlineData(null, "Test123")]
    [InlineData("Test123", "Test123")]
    public async Task ChangePasswordCommandValidator_WhenValidData_ShouldHaveValidationError(string oldPassword,
        string newPassword)
    {
        var command = new ChangePasswordCommand(oldPassword, newPassword);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrors();
    }
    
    // ERROR TESTS END
}