using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Common.Extensions;

public static class IdentityResultExtensions
{
    public static void ThrowIfFailed(this IdentityResult result)
    {
        if(result.Succeeded) return;

        var conflictError = result.Errors.FirstOrDefault(e => e.Code.StartsWith("Duplicate"));
        if (conflictError is not null)
        {
            throw new ConflictException(conflictError.Description);
        }
        
        var identityValidationErrors = result.Errors
            .Where(e => e.Code.StartsWith("Password") || e.Code.StartsWith("Invalid"))
            .ToList();

        if (identityValidationErrors.Any())
        {
            var failures = identityValidationErrors.Select(error =>
            {
                string propertyName = error.Code.StartsWith("Password") ? "Password" : "User";
                
                return new ValidationFailure(propertyName, error.Description)
                {
                    ErrorCode = error.Code
                };
            });
            throw new ValidationException(failures);
        }
        
        var genericError = result.Errors.First();
        throw new Exception(genericError.Description);
    }
}