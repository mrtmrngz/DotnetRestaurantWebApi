using MediatR;
using Microsoft.AspNetCore.Http;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Commands.CreateProductCommand;

public record CreateProductCommand(
    string Title,
    string? Description,
    decimal Price,
    Guid CategoryId,
    IFormFileCollection Images
    ): IRequest<ProductCreateUpdateResponse>;