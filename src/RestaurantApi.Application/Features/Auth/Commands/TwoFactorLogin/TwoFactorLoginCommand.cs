using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.TwoFactorLogin;

public record TwoFactorLoginCommand(string Otp): IRequest<LoginResponse>;