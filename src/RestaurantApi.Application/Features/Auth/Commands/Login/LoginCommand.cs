using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
    ): IRequest<LoginResponse>;