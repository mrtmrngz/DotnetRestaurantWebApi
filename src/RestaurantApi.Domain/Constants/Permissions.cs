using System.ComponentModel;

namespace RestaurantApi.Domain.Constants;

public static class Permissions
{
    public static class UserPermissions
    {
        [Description("Kullanıcıları görüntüleme yetkisi verir.")]
        public const string View = "user:view";
        [Description("Kullanıcı oluşturma yetkisi verir.")]
        public const string Create = "user:create";
        [Description("Kullanıcı güncelleme yetkisi verir.")]
        public const string Update = "user:update";
        [Description("Kullanıcı silme yetkisi verir.")]
        public const string Delete = "user:delete";
    }
    
    public static class CategoryPermissions
    {
        [Description("Kategorileri görüntüleme yetkisi verir.")]
        public const string View = "category:view";
        [Description("Kategori oluşturma yetkisi verir.")]
        public const string Create = "category:create";
        [Description("Kategori güncelleme yetkisi verir.")]
        public const string Update = "category:update";
        [Description("Kategori silme yetkisi verir.")]
        public const string Delete = "category:delete";
    }
    
    public static class ProductPermissions
    {
        [Description("Ürünleri görüntüleme yetkisi verir.")]
        public const string View = "product:view";
        [Description("Ürün oluşturma yetkisi verir.")]
        public const string Create = "product:create";
        [Description("Ürün güncelleme yetkisi verir.")]
        public const string Update = "product:update";
        [Description("Ürün silme yetkisi verir.")]
        public const string Delete = "product:delete";
    }
}