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
}