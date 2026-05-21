using FluentValidation;

namespace RestaurantApi.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator: AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email alanı boş olamaz.")
            .EmailAddress().WithMessage("Geçersiz email adresi.");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola alanı zorunludur.")
            .Matches("[A-Z]").WithMessage("Parolada en az 1 adet büyük harf olmalıdır.")
            .MinimumLength(6).WithMessage("Parola en az 6 karakter uzunluğunda olmalıdır.");
    }
}