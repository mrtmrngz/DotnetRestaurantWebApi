using FluentValidation;
using RestaurantApi.Application.Common.Extensions;

namespace RestaurantApi.Application.Features.Address.Commands.UpdateAddressCommand;

public class UpdateAddressCommandValidator: AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneFieldToUpdate)
            .WithMessage("Güncelleme yapmak için en az bir alan belirtmelisiniz.")
            .WithName("UpdateAddress");
        
        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Adres başlığı girmek zorundasınız.")
                .MaximumLength(50).WithMessage("Adres başlığı en fazla 50 karakter olabilir.");
        });
        
        When(x => x.RecipientName != null, () =>
        {
            RuleFor(x => x.RecipientName)
                .NotEmpty().WithMessage("Alıcı kişi adı boş bırakılamaz.")
                .MaximumLength(100).WithMessage("Alıcı kişi adı en fazla 100 karakter olabilir.");
        });

        When(x => x.City != null, () =>
        {
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Şehir boş bırakılamaz.")
                .MaximumLength(50).WithMessage("Şehir en fazla 50 karakter olabilir.");
        });

        When(x => x.Town != null, () =>
        {
            RuleFor(x => x.Town)
                .NotEmpty().WithMessage("İlçe adı boş bırakılamaz.")
                .MaximumLength(100).WithMessage("İlçe adı en fazla 100 karakter olabilir.");
        });

        When(x => x.Neighborhood != null, () =>
        {
            RuleFor(x => x.Neighborhood)
                .NotEmpty().WithMessage("Mahalle adı boş bırakılamaz.")
                .MaximumLength(70).WithMessage("Mahalle adı en fazla 70 karakter olabilir.");
        });

        When(x => x.Street != null, () =>
        {
            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Sokak adı boş bırakılamaz.")
                .MaximumLength(85).WithMessage("Sokak adı en fazla 85 karakter olabilir.");
        });

        When(x => x.BuildingInfo != null, () =>
        {
            RuleFor(x => x.BuildingInfo)
                .MaximumLength(150).WithMessage("Bina bilgisi en fazla 150 karakter olabilir.");
        });

        When(x => x.BuildingNumber != null, () =>
        {
            RuleFor(x => x.BuildingNumber)
                .NotEmpty().WithMessage("Bina numarası boş bırakılamaz.")
                .MaximumLength(15).WithMessage("Bina numarası en fazla 15 karakter olabilir.");
        });

        When(x => x.PhoneNumber != null, () =>
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Telefon numarası boş bırakılamaz.")
                .MaximumLength(15).WithMessage("Telefon numarası maksimum 15 karakter olmalıdır.")
                .BeValidTurkishNumber();
        });

        When(x => x.ZipCode != null, () =>
        {
            RuleFor(x => x.ZipCode)
                .MaximumLength(10).WithMessage("Posta kodu en fazla 10 karakter olabilir.");
        });
    }

    private static bool HasAtLeastOneFieldToUpdate(UpdateAddressCommand command)
    {
        return 
            command.Title != null ||
            command.RecipientName != null ||
            command.City != null ||
            command.Town != null ||
            command.Neighborhood != null ||
            command.Street != null ||
            command.BuildingInfo != null ||
            command.BuildingNumber != null ||
            command.PhoneNumber != null ||
            command.IsDefault != null ||
            command.ZipCode != null;
    }
}