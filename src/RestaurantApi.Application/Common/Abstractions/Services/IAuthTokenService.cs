using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface IAuthTokenService
{
    Task<JwtAndRefreshTokenResult> CreateRefreshAndAccessTokenService(AppUser user);
}