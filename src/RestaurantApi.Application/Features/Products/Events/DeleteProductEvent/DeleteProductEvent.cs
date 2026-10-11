using MediatR;

namespace RestaurantApi.Application.Features.Products.Events.DeleteProductEvent;

public record DeleteProductEvent(Guid ProductId) : INotification;
