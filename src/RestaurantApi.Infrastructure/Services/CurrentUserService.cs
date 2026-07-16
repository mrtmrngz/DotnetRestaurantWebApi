using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Infrastructure.Services;

public class CurrentUserService: ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst("Id")?.Value 
                             ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    public Guid GetRequiredUserId()
    {
        var userId = UserId;

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guidUserId))
        {
            throw new UnauthorizedException("Geçersiz veya eksik oturum bilgisi. Lütfen tekrar giriş yapınız.");
        }
        
        return guidUserId;
    }
}