using MediatR;

namespace RestaurantApi.Application.Features.Category.Events.UpdateCategoryEvent;

public record UpdateCategoryEvent(string OldPublicId, Guid CategoryId): INotification;