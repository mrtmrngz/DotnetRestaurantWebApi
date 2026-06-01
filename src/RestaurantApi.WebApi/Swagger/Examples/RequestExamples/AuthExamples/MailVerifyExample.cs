using RestaurantApi.Application.Features.Auth.Commands.MailVerify;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AuthExamples;

public class MailVerifyExample: IExamplesProvider<MailVerifyCommand>
{
    public MailVerifyCommand GetExamples()
    {
        return new MailVerifyCommand(
            Token:
            "CfDJ8MXuWvq7V9hGvNzZ2YvPmxA3K9zLpQ9w2eR7tY8uI0oO1pP2qQ3w4eR5tY6uI7oO8pP9qQ0w1eR2tY3uI4oO5pP6qQ7w8eR9tY0uI1oO2pP3qQ4w5eR6tY7uI8oO9pP0qQ==");
    }
}