using FluentValidation;
using RestaurantApi.Application.Common.Extensions;

namespace RestaurantApi.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator: AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İsim alanı boş olamaz.")
            .MaximumLength(60).WithMessage("İsim alanı 60 karakterden fazla olamaz.");
        
        RuleFor(x => x.Surname)
            .NotEmpty().WithMessage("Soyad alanı boş olamaz.")
            .MaximumLength(60).WithMessage("Soyad alanı 60 karakterden fazla olamaz.");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email alanı boş olamaz.")
            .EmailAddress().WithMessage("Geçersiz email adresi.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola alanı zorunludur.")
            .Matches("[A-Z]").WithMessage("Parolada en az 1 adet büyük harf olmalıdır.")
            .MinimumLength(6).WithMessage("Parola en az 6 karakter uzunluğunda olmalıdır.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası boş olamaz")
            .MaximumLength(15).WithMessage("Telefon numarası maksimum 15 karakter olmalıdır.")
            .BeValidTurkishNumber();
    }
}