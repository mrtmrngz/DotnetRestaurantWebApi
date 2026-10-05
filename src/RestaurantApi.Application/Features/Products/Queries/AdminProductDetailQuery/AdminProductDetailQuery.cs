using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.AdminProductDetailQuery;

public record AdminProductDetailQuery(): IRequest<GeneralSuccessResponseWithData<AdminProductDetailQueryResult>>
{
    [JsonIgnore] 
    public Guid ProductId { get; init; }
};