using MediatR;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Events;

public record UserTwoFactorAuthEvent(AppUser User, string Otp, string Email, string FullName): INotification;