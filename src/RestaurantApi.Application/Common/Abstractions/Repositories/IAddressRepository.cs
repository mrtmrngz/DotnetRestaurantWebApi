using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IAddressRepository
{
    Task<int> UserAddressCount(Guid userId, CancellationToken cancellationToken = default);
    void CreateAddress(Address address);
    Task UpdateOtherDefaultAddressToFalse(Guid userId, CancellationToken ctx);
    Task<IReadOnlyList<Address>> GetUserAddressList(Guid userId, CancellationToken ctx);
}