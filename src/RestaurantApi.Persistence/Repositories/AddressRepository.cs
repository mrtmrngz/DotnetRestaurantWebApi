using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Dtos.AddressDtos;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class AddressRepository: IAddressRepository
{
    private readonly ApiContext _context;

    public AddressRepository(ApiContext context)
    {
        _context = context;
    }

    public async Task<int> UserAddressCount(Guid userId, CancellationToken ctx = default)
    {
        return await _context.Addresses.CountAsync(add => add.UserId == userId && !add.IsDeleted, ctx);
    }

    public void CreateAddress(Address address)
    {
        _context.Addresses.Add(address);
    }

    public async Task UpdateOtherDefaultAddressToFalse(Guid userId, CancellationToken ctx)
    {
        await _context.Addresses
            .Where(add => add.UserId == userId && add.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsDefault, false), ctx);
    }

    public async Task<IReadOnlyList<Address>> GetUserAddressList(Guid userId, CancellationToken ctx)
    {
        return await _context.Addresses
            .AsNoTracking()
            .Where(add => add.UserId == userId && !add.IsDeleted)
            .OrderByDescending(add => add.IsDefault)
            .ThenByDescending(add => add.CreatedAt)
            .ToListAsync(ctx);
    }

    public async Task<Address?> FindUserActiveAddress(Guid userId, Guid addressId, CancellationToken ctx)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(add => add.UserId == userId && add.Id == addressId && !add.IsDeleted, ctx);
    }
}