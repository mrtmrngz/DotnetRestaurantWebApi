using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.CreateRestaurantTableCommand;

public record CreateRestaurantTableCommand(int Number, int Capacity): IRequest<BaseResponse>;
