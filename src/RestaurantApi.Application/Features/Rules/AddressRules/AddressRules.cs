using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;

namespace RestaurantApi.Application.Features.Rules.AddressRules;

public class AddressRules
{
    private readonly ILogger<AddressRules> _logger;

    public AddressRules(ILogger<AddressRules> logger)
    {
        _logger = logger;
    }

    public Task ShouldAddressExist(Domain.Entities.Address? address, Guid userId, Guid addressId)
    {
        if (address is null)
        {
            _logger.LogWarning(
                "Aranan adres veritabanında bulunamadı. AddressId: {AddressId}, UserId: {UserId}", 
                addressId, userId);
            throw new NotFoundException("Adres bulunamadı.");
        }

        return Task.CompletedTask;
    }
    
    public Task ShouldAddressExistInCache(GetUserAddressQueryResult? address, Guid userId, Guid addressId)
    {
        if (address is null)
        {
            _logger.LogWarning(
                "Aranan adres Redis cache listesinde bulunamadı (Soft-Deleted veya geçersiz ID). AddressId: {AddressId}, UserId: {UserId}", 
                addressId, userId);
            throw new NotFoundException("Adres bulunamadı.");
        }

        return Task.CompletedTask;
    }
}