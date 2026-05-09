using FluentValidation.Results;

namespace RestaurantApi.Application.Common.Exceptions;

public class ValidationException: Exception
{
    public List<ValidationFailure> Errors { get; }

    public ValidationException() : base("Bir veya birden fazla doğrulama hatası oluştu.")
    {
        Errors = new List<ValidationFailure>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures.ToList();
    }
}