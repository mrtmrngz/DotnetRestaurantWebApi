using Microsoft.AspNetCore.Identity;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Repositories;

public class SigninManagerRepository: ISigninManager
{
    private readonly SignInManager<AppUser> _signInManager;

    public SigninManagerRepository(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password)
    {
        return await _signInManager.CheckPasswordSignInAsync(user, password, true);
    }
}