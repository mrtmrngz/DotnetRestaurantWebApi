using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Roles.Commands;

public sealed record CreateRoleCommand(string Name) : IRequest<BaseResponse>;
