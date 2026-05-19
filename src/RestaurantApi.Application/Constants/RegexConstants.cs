namespace RestaurantApi.Application.Constants;

public static class RegexConstants
{
    public const string TurkishMobilePath = @"^(?:\+90.?5|0090.?5|905|0?5)(?:[01345][0-9])\s?(?:[0-9]{3})\s?(?:[0-9]{2})\s?(?:[0-9]{2})$";
    public const string TurkishLandlinePath = @"^(0)([2348]{1})([0-9]{2})\s?([0-9]{3})\s?([0-9]{2})\s?([0-9]{2})$";
}