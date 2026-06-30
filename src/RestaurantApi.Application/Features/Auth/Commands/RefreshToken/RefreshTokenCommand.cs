using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string? Token): IRequest<LoginResponse>;