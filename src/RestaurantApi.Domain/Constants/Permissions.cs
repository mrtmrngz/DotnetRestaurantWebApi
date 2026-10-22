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

    public static class DiscountPermissions
    {
        [Description("İndirimleri görüntüleme yetkisi verir.")]
        public const string View = "discount:view";
        [Description("İndirim oluşturma yetkisi verir.")]
        public const string Create = "discount:create";
        [Description("İndirim güncelleme yetkisi verir.")]
        public const string Update = "discount:update";
        [Description("İndirim silme yetkisi verir.")]
        public const string Delete = "discount:delete";
    }

    public static class TablePermissions
    {
        [Description("Masaları görüntüleme yetkisi verir.")]
        public const string View = "table:view";
        [Description("Masa oluşturma yetkisi verir.")]
        public const string Create = "table:create";
        [Description("Masa güncelleme yetkisi verir.")]
        public const string Update = "table:update";
        [Description("Masa silme yetkisi verir.")]
        public const string Delete = "table:delete";
    }

    public static class ReservationPermissions
    {
        [Description("Rezervasyon oluşturma yetkisi verir.")]
        public const string Create = "reservation:create";
        [Description("Panel Rezervasyon oluşturma yetkisi verir.")]
        public const string PanelCreate = "reservation:create:panel";
        [Description("Panel rezervasyon iptal etme yetkisi verir.")]
        public const string Cancel = "reservation:cancel";
        [Description("Rezervasyon durumu güncelleme yetkisi verir.")]
        public const string StatusUpdate = "reservation:status-update";
        [Description("Panelde yetki sahibi kullanıcılara, tüm rezervasyonları listeleme yetkisi verir.")]
        public const string PanelListing = "reservation:listing";
    }
    
    public static class CartPermissions
    {
        [Description("Sepete ekleme ve sepet oluşturma yetkisi verir.")]
        public const string AddToCart = "cart:add";
        [Description("Sepetteki ürünlerin miktarını güncelleme verir.")]
        public const string UpdateCart = "cart:update";
        [Description("Sepeti silme yetkisi verir.")]
        public const string DeleteCartItems = "cart:items:delete";
        public const string DeleteCart = "cart:delete";
    }
}