using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Commands.UpdateProductCommand;

public record UpdateProductCommand(
    string? Title = null,
    string? Description = null,
    decimal? Price = null,
    Guid? CategoryId = null,
    IFormFileCollection? Images = null,
    IReadOnlyList<string>? DeletedImagePublicIds = null
) : IRequest<ProductCreateUpdateResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid ProductId { get; init; }
};