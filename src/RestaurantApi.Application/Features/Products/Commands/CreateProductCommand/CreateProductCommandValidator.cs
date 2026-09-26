using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace RestaurantApi.Application.Features.Products.Commands.CreateProductCommand;

public class CreateProductCommandValidator: AbstractValidator<CreateProductCommand>
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
    
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .NotNull().WithMessage("Ürün adı zorunludur.")
            .MinimumLength(2).WithMessage("Ürün adı en az 2 karakter olmalıdır.")
            .MaximumLength(150).WithMessage("Ürün adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Ürün açıklaması en fazla 500 karakter olabilir.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(10000)
            .WithMessage("Ürün fiyatı 10.000'den küçük olmalıdır");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Ürün kategori id boş olamaz.")
            .NotNull().WithMessage("Ürün kategori id zorunludur.");
        
        RuleFor(x => x.Images)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("En az 1 ürün görseli yüklenmelidir.")
            .Must(images => images.Count <= MaxImageCount)
            .WithMessage($"En fazla {MaxImageCount} ürün görseli yüklenebilir.");
        
        RuleForEach(x => x.Images)
            .Must(BeValidFile)
            .WithMessage("Geçersiz dosya. Dosya boş veya bozuk olabilir.")
            .Must(BeValidSize)
            .WithMessage("Her ürün görselinin boyutu 0'dan büyük ve en fazla 5 MB olabilir.")
            .Must(BeValidExtension)
            .WithMessage("Desteklenen görsel formatları: JPG, JPEG, PNG ve WEBP.")
            .Must(BeValidContentType)
            .WithMessage("Geçersiz görsel içerik türü.");
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
}