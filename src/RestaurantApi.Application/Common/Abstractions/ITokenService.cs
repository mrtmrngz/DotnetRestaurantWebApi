using System.Security.Claims;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions;

public interface ITokenService
{
    Task<string> CreateAccessToken(AppUser user, IList<string> roles);
}