using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Name,
    string Surname,
    string Email,
    string Password,
    string PhoneNumber
): IRequest<BaseResponse>;