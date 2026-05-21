using Microsoft.AspNetCore.Identity;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface ISigninManager
{
    Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password);
}