using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace RestaurantApi.Application.Features.Products.Commands.UpdateProductCommand;

public class UpdateProductCommandValidator: AbstractValidator<UpdateProductCommand>
{
    private static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const int MaxImageCount = 10;
    
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneFieldToUpdate)
            .WithMessage("Güncellenecek en az bir alan belirtilmelidir.");

        RuleFor(x => x.Title)
            .MinimumLength(2)
            .WithMessage("Ürün adı en az 2 karakter olmalıdır.")
            .MaximumLength(150)
            .WithMessage("Ürün adı en fazla 150 karakter olabilir.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Ürün açıklaması en fazla 500 karakter olabilir.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(10000)
            .WithMessage("Ürün fiyatı 10.000'den küçük veya eşit olmalıdır.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.CategoryId)
            .Must(categoryId => categoryId != Guid.Empty)
            .WithMessage("Ürün kategori id boş olamaz.")
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.Images)
            .Cascade(CascadeMode.Stop)
            .Must(images => images!.Count > 0)
            .WithMessage("Yüklenen görsel listesi boş olamaz.")
            .Must(images => images!.Count <= MaxImageCount)
            .WithMessage($"En fazla {MaxImageCount} ürün görseli yüklenebilir.")
            .When(x => x.Images is not null);

        RuleForEach(x => x.Images)
            .Cascade(CascadeMode.Stop)
            .Must(BeValidFile)
            .WithMessage("Geçersiz dosya. Dosya boş veya bozuk olabilir.")
            .Must(BeValidSize)
            .WithMessage("Her ürün görselinin boyutu 0'dan büyük ve en fazla 5 MB olabilir.")
            .Must(BeValidExtension)
            .WithMessage("Desteklenen görsel formatları: JPG, JPEG, PNG ve WEBP.")
            .Must(BeValidContentType)
            .WithMessage("Geçersiz görsel içerik türü.");

        RuleForEach(x => x.DeletedImagePublicIds)
            .NotEmpty()
            .WithMessage("Silinecek görsel public id'si boş olamaz.");
    }
    
    private static bool BeValidFile(IFormFile file)
    {
        return file != null 
               && file.Length > 0 
               && !string.IsNullOrWhiteSpace(file.FileName)
               && !string.IsNullOrWhiteSpace(file.ContentType);
    }
    
    private static bool BeValidSize(IFormFile file)
    {
        return file.Length > 0 && file.Length <= MaxFileSizeBytes;
    }

    private static bool BeValidExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);

        return AllowedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidContentType(IFormFile file)
    {
        return AllowedContentTypes.Contains(
            file.ContentType,
            StringComparer.OrdinalIgnoreCase);
    }
    
    private static bool HasAtLeastOneFieldToUpdate(UpdateProductCommand command)
    {
        return command.Title is not null
               || command.Description is not null
               || command.Price.HasValue
               || command.CategoryId.HasValue
               || (command.Images is not null && command.Images.Count > 0)
               || (command.DeletedImagePublicIds is not null
                   && command.DeletedImagePublicIds.Count > 0);
    }
}