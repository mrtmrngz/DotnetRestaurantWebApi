using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IDiscountRepository
{
    IQueryable<Discount> GetAllAsQueryable();
    void AddDiscount(Discount discount, CancellationToken ctx);
}
