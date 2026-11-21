namespace RestaurantApi.WebApi.Configurations.RateLimiting.Policies;

public static class RateLimitPolicies
{
    public static class Auth
    {
        public const string Register = "Auth.Register";
        public const string General = "Auth.General";
        public const string Refresh = "Auth.Refresh";
        public const string MailVerify = "Auth.MailVerify";
    }
    
    public static class Password
    {
        public const string ChangeVerify = "Password.ChangeVerify";
        public const string ChangeApply = "Password.ChangeApply";
    }
    
    public static class SameUser
    {
        public const string Standard = "SameUser.Standard";
        public const string Strict = "SameUser.Strict";
        public const string Restricted = "SameUser.Restricted";
        public const string Critical = "SameUser.Critical";
        public const string Flexible = "SameUser.Flexible";
    }
    
    public static class Panel
    {
        public const string GeneralRead = "Panel.GeneralRead";
        public const string Put = "Panel.Put";
        public const string GeneralPost = "Panel.GeneralPost";
        public const string GeneralPatch = "Panel.GeneralPatch";
        public const string GeneralDelete = "Panel.GeneralDelete";
        public const string StrictPatch = "Panel.StrictPatch";
        public const string FlexiblePatch = "Panel.FlexiblePatch";
        public const string Order = "Panel.Order";
    }
    
    public static class Cart
    {
        public const string GeneralCart = "Cart.GeneralCart";
        public const string UserRemoveCart = "Cart.UserRemoveCart";
    }
    
    public static class Checkout
    {
        public const string HybridPanel = "Checkout.HybridPanel";
        public const string User = "Checkout.User";
    }
    
    public static class Categories
    {
        public const string GetCategories = "Categories.GetCategories";
    }
    
    public static class Public
    {
        public const string Flexible = "Public.Flexible";
    }
}
