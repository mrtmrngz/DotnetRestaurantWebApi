using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Rules.UserRules;

public class UserRules
{
    public Task UserShouldExist404(AppUser? user)
    {
        if (user == null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        return Task.CompletedTask;
    }
}