using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;

public record PublicProductDetailQuery() : IRequest<GeneralSuccessResponseWithData<PublicProductDetailQueryResult>>
{
    [JsonIgnore] 
    public string Slug { get; init; } = null!;
};