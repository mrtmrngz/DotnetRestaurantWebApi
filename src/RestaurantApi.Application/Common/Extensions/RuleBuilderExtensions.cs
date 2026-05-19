using System.Text.RegularExpressions;
using FluentValidation;
using RestaurantApi.Application.Constants;

namespace RestaurantApi.Application.Common.Extensions;

public static class RuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, string> BeValidTurkishNumber<T>(this IRuleBuilderOptions<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(phone => 
                !string.IsNullOrEmpty(phone) &&
                (Regex.IsMatch(phone, RegexConstants.TurkishLandlinePath) || 
                 Regex.IsMatch(phone, RegexConstants.TurkishMobilePath)
                 )
            ).WithMessage("Lütfen geçerli bir telefon numarası giriniz.");
    }
}