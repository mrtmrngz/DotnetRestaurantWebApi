using MediatR;
using Microsoft.AspNetCore.Http;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;

public record CreateCategoryCommand(string Title, IFormFile Image): IRequest<BaseResponse>;