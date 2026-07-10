using MediatR;

namespace RestaurantApi.Application.Features.Auth.Events;

public record ForgotPasswordVerifyEvent(string UserId, string Token, string Email, string FullName, int ExpiresInMinute): INotification;