using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace RestaurantApi.Application.Features.Category.Commands.UpdateCategoryCommand;

public class UpdateCategoryCommandValidator: AbstractValidator<UpdateCategoryCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneFieldToUpdate)
            .WithMessage("Güncelleme yapmak için en az bir alan belirtmelisiniz.")
            .WithName("UpdateCategory");
        
        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Kategori adı boş bırakılamaz.")
                .MinimumLength(2).WithMessage("Kategori adı en az 2 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.");
        });
        
        When(x => x.Image != null, () =>
        {
            RuleFor(x => x.Image)
                .Must(BeValidSize).WithMessage("Görsel boyutu en fazla 5MB olabilir.")
                .Must(BeValidExtension).WithMessage("Sadece .jpg, .jpeg, .png veya .webp formatında görseller yüklenebilir.");
        });
    }
    
    private static bool BeValidSize(IFormFile? file)
    {
        if (file is null) return true;
        return file.Length <= MaxFileSizeBytes;
    }

    private static bool BeValidExtension(IFormFile? file)
    {
        if (file is null) return true;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
    
    private static bool HasAtLeastOneFieldToUpdate(UpdateCategoryCommand command)
    {
        return command.Title != null ||
               command.Image != null;
    }
}