using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Kategori adı boş olamaz.")
            .NotNull().WithMessage("Kategori adı zorunludur.")
            .MinimumLength(2).WithMessage("Kategori adı en az 2 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.Image)
            .NotNull().WithMessage("Kategori görseli yüklemek zorunludur.")
            .DependentRules(() =>
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
}