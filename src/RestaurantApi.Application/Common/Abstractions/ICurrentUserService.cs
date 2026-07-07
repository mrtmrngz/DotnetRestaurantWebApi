namespace RestaurantApi.Application.Common.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
}