using FluentValidation;

namespace RestaurantApi.Application.Features.ProfileHandlers.Commands.ChangePasswordCommand;

public class ChangePasswordCommandValidator: AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Eski parolanızı girmek zorundasınız.")
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Eski parolanız sadece boşluklardan oluşamaz.");
        
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni parola girmek zorundasınız.")
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Yeni parolanız sadece boşluklardan oluşamaz.")
            .MinimumLength(6).WithMessage("Yeni parola en az 6 karakter uzunluğunda olmalıdır.")
            .Matches("[A-Z]").WithMessage("Yeni parolanızda en az 1 adet büyük harf olmalıdır.")
            .Matches("[0-9]").WithMessage("Yeni parolanızda en az 1 adet rakam (sayı) olmalıdır.")
            .NotEqual(x => x.OldPassword).WithMessage("Yeni parolanız ile eski parolanız aynı olamaz.");
    }
}