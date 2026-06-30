using FluentValidation;

namespace RestaurantApi.Application.Features.Auth.Commands.TwoFactorLogin;

public class TwoFactorLoginCommandValidator: AbstractValidator<TwoFactorLoginCommand>
{
    public TwoFactorLoginCommandValidator()
    {
        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Doğrulama kodu boş geçilemez.")
            .NotNull().WithMessage("Doğrulama kodu boş geçilemez.")
            .Matches(@"^[0-9]+$").WithMessage("Doğrulama kodu sadece rakamlardan oluşmalıdır.")
            .Length(6).WithMessage("Doğrulama kodu tam olarak 6 haneli olmalıdır.");
    }
}