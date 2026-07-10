using FluentValidation;

namespace RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;

public class ForgotPasswordCommandValidator: AbstractValidator<ForgotPasswordVerifyCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email alanı boş olamaz.")
            .EmailAddress().WithMessage("Geçersiz email adresi.");
    }
}