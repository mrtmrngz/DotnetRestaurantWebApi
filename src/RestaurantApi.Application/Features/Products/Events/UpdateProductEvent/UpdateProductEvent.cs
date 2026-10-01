using MediatR;

namespace RestaurantApi.Application.Features.Products.Events.UpdateProductEvent;

public record UpdateProductEvent(IReadOnlyList<string> PublicIDs) : INotification;