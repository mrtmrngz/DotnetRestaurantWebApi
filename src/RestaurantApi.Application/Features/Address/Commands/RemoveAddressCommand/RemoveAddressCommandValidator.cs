using FluentValidation;

namespace RestaurantApi.Application.Features.Address.Commands.RemoveAddressCommand;

public class RemoveAddressCommandValidator: AbstractValidator<RemoveAddressCommand>
{
    public RemoveAddressCommandValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty()
            .WithMessage("Silinecek adres bilgisi (AddressId) boş olamaz.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı kimlik bilgi (UserId) bulunamadı.");
    }
}