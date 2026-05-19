using MediatR;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Events;

public record UserRegisteredEvent(AppUser User, string Token, string Email, string FullName): INotification;