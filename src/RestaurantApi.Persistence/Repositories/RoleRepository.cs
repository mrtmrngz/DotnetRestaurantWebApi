using Microsoft.AspNetCore.Identity;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Repositories;

public class RoleRepository: IRoleRepository
{

    private readonly RoleManager<AppRole> _roleManager;

    public RoleRepository(RoleManager<AppRole> roleManager)
    {
        _roleManager = roleManager;
    }
}