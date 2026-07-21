using FluentValidation;
using RestaurantApi.Application.Common.Extensions;

namespace RestaurantApi.Application.Features.Address.Commands;

public class CreateAddressCommandValidator: AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Adres başlığı girmek zorundasınız.")
            .MaximumLength(50).WithMessage("Adres başlığı en fazla 50 karakter olabilir.");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Alıcı kişi adını girmek zorundasınız.")
            .MaximumLength(100).WithMessage("Alıcı kişi adı en fazla 100 karakter olabilir.");
        
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir girmek zorundasınız.")
            .MaximumLength(50).WithMessage("Şehir en fazla 50 karakter olabilir.");
        
        RuleFor(x => x.Town)
            .NotEmpty().WithMessage("İlçe adı girmek zorundasınız.")
            .MaximumLength(100).WithMessage("İlçe adı en fazla 100 karakter olabilir.");
        
        RuleFor(x => x.Neighborhood)
            .NotEmpty().WithMessage("Mahalle adı girmek zorundasınız.")
            .MaximumLength(70).WithMessage("Mahalle adı en fazla 70 karakter olabilir.");
        
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Sokak adı girmek zorundasınız.")
            .MaximumLength(85).WithMessage("Sokak adı en fazla 85 karakter olabilir.");
        
        RuleFor(x => x.BuildingInfo)
            .MaximumLength(150).WithMessage("Bina bilgisi en fazla 150 karakter olabilir.");
        
        RuleFor(x => x.BuildingNumber)
            .NotEmpty().WithMessage("Bina numarası girmek zorundasınız.")
            .MaximumLength(15).WithMessage("Bina numarası en fazla 15 karakter olabilir.");
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası boş olamaz")
            .MaximumLength(15).WithMessage("Telefon numarası maksimum 15 karakter olmalıdır.")
            .BeValidTurkishNumber();
    }
}