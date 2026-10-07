using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Commands.DeleteProductCommand;

public record DeleteProductCommand(): IRequest<BaseResponse>
{
    [JsonIgnore]
    public Guid ProductId { get; init; }
}
