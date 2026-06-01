using MediatR;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Events;

public record MailVerifiedEvent(AppUser User, string Token): INotification;