using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Commands.DeleteCategoryCommand;

public record DeleteCategoryCommand(Guid CategoryId): IRequest<BaseResponse>;