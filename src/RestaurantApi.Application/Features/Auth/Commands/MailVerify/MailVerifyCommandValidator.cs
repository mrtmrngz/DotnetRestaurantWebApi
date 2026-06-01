using FluentValidation;

namespace RestaurantApi.Application.Features.Auth.Commands.MailVerify;

public class MailVerifyCommandValidator: AbstractValidator<MailVerifyCommand>
{
    public MailVerifyCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Doğrulama token'ı boş bırakılamaz.")
            .MinimumLength(20).WithMessage("Geçersiz token formatı.");
    }
}