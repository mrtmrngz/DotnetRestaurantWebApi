using FluentValidation;

namespace RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordApply;

public class ForgotPasswordApplyCommandValidator: AbstractValidator<ForgotPasswordApplyCommand>
{
    public ForgotPasswordApplyCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Doğrulama token'ı boş bırakılamaz.")
            .MinimumLength(20).WithMessage("Geçersiz token formatı.");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola alanı zorunludur.")
            .Matches("[A-Z]").WithMessage("Parolada en az 1 adet büyük harf olmalıdır.")
            .MinimumLength(6).WithMessage("Parola en az 6 karakter uzunluğunda olmalıdır.");
    }
}