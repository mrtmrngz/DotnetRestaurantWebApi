
namespace RestaurantApi.Application.Common;

public static class CacheKeys
{
    public static string RefreshToken(string token) => $"refreshToken:{token}";
    public static string RolePermissions(string role) => $"role:{role}:permissions";
}