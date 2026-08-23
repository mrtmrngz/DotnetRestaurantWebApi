using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Services;
using Slugify;

namespace RestaurantApi.Infrastructure.Services;

public class SlugService : ISlugService
{
    private readonly ISlugHelper _slugHelper;

    public SlugService()
    {
        var config = new SlugHelperConfiguration();

        config.StringReplacements.Add("ç", "c");
        config.StringReplacements.Add("Ç", "c");
        config.StringReplacements.Add("ğ", "g");
        config.StringReplacements.Add("Ğ", "g");
        config.StringReplacements.Add("ı", "i");
        config.StringReplacements.Add("I", "i");
        config.StringReplacements.Add("İ", "i");
        config.StringReplacements.Add("ö", "o");
        config.StringReplacements.Add("Ö", "o");
        config.StringReplacements.Add("ş", "s");
        config.StringReplacements.Add("Ş", "s");
        config.StringReplacements.Add("ü", "u");
        config.StringReplacements.Add("Ü", "u");

        config.ForceLowerCase = true;

        _slugHelper = new SlugHelper(config);
    }

    public string ToSlug(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return _slugHelper.GenerateSlug(text);
    }

    public async Task<string> GenerateUniqueSlugAsync<TEntity>(IQueryable<TEntity> query, string title,
        CancellationToken ctx = default) where TEntity : class
    {
        var baseSlug = ToSlug(title);
        var uniqueSlug = baseSlug;
        int count = 1;

        while (await query.AnyAsync(BuildSlugPredicate<TEntity>(uniqueSlug), ctx))
        {
            uniqueSlug = $"{baseSlug}-{count}";
            count++;
        }

        return uniqueSlug;
    }

    private static Expression<Func<TEntity, bool>> BuildSlugPredicate<TEntity>(string slugValue)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, "Slug");
        var equals = Expression.Equal(property, Expression.Constant(slugValue));
        return Expression.Lambda<Func<TEntity, bool>>(equals, parameter);
    }
}