using System.Runtime.InteropServices;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordApply;

namespace RestaurantApi.UnitTests.Features.Auth.Command;

public class ForgotPasswordApplyCommandValidatorTests
{
    private readonly ForgotPasswordApplyCommandValidator _validator;

    public ForgotPasswordApplyCommandValidatorTests()
    {
        _validator = new ForgotPasswordApplyCommandValidator();
    }
    
    // ERROR TEST START

    [Theory]
    [InlineData("invalid-token", "")]
    [InlineData("", "1234")]
    public async Task ForgotPasswordApplyCommandValidator_WhenFieldsAreNotPass_ShouldHaveValidationError(string token,
        string password)
    {
        var command = new ForgotPasswordApplyCommand(token, password);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("invalid-token")]
    public async Task ForgotPasswordApplyCommandValidator_WhenTokenFieldAreNotPass_ShouldHaveValidationError(string token)
    {
        var pass = "Test123";
        var command = new ForgotPasswordApplyCommand(token, pass);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("invalid-password")]
    public async Task ForgotPasswordApplyCommandValidator_WhenPasswordFieldAreNotPass_ShouldHaveValidationError(string password)
    {
        var token = Guid.NewGuid().ToString();
        var command = new ForgotPasswordApplyCommand(token, password);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
    
    // ERROR TEST END

    // SUCCESS TEST START

    [Theory]
    [InlineData(
        "CfDJ8MXuWvq7V9hGvNzZ2YvPmxA3K9zLpQ9w2eR7tY8uI0oO1pP2qQ3w4eR5tY6uI7oO8pP9qQ0w1eR2tY3uI4oO5pP6qQ7w8eR9tY0uI1oO2pP3qQ4w5eR6tY7uI8oO9pP0qQ==",
        "Secret123")]
    [InlineData(
        "CfDJ8MXuWvq7V9hGvNzZ2YvPmxA3K9zLpQ9w2eR7tY8uI0oO1pP2qQ3w4eR5tY6uI7oO8pP9qQ0w1eR2tY3uI4oO5pP6qQ7w8eR9tY0uI1oO2pP3qQ4w5eR6tY7uI8oO9pP0qQ==",
        "Test123")]
    [InlineData(
        "CfDJ8MXuWvq7V9hGvNzZ2YvPmxA3K9zLpQ9w2eR7tY8uI0oO1pP2qQ3w4eR5tY6uI7oO8pP9qQ0w1eR2tY3uI4oO5pP6qQ7w8eR9tY0uI1oO2pP3qQ4w5eR6tY7uI8oO9pP0qQ==",
        "Admin123")]
    public async Task ForgotPasswordApplyCommandValidator_WhenEveryFieldsArePass_ShouldNotHaveValidationError(
        string token, string password)
    {
        var command = new ForgotPasswordApplyCommand(token, password);

        var result = _validator.TestValidate(command);
        
        result.ShouldNotHaveAnyValidationErrors();
    }

    // SUCCESS TEST END
}