using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.ProductMediaRules;

public class ProductMediaRules
{
    private readonly ILogger<ProductMediaRules> _logger;

    public ProductMediaRules(ILogger<ProductMediaRules> logger)
    {
        _logger = logger;
    }


    public void ShouldDeletedImageCountCantEqualCurrentImageCount(int newImageCount, int deletedImgCount, int currCount)
    {
        if (newImageCount == 0 && currCount == deletedImgCount)
        {
            throw new UnprocessableEntityError("Ürün resimsiz olamaz.");
        }
    }

    public void ShouldDeletedImgCountMatchesPrdDeletedImgCount(int count1, int count2)
    {
        if (count1 != count2)
        {
            throw new BadRequestException(
                "Silinmek istenen görseller bu ürüne ait değil.");
        }
    }

    public void ShouldImageCountNotExceedMaximum(int count)
    {
        if (count > 10)
        {
            throw new UnprocessableEntityError("Bir üründe en fazla 10 adet resim olabilir.");
        }
    }
}