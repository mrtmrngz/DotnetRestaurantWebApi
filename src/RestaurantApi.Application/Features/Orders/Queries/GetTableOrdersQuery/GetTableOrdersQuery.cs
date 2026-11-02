using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;

// Bir masanın siparişlerini listeleme sorgusu (panel akışı).
// TableId controller tarafından route'tan set edilir; istemcinin query string/gövde ile
// başka bir masa seçmesini engellemek için [JsonIgnore] + [BindNever] ile korunur.
public record GetTableOrdersQuery: IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>>
{
    [JsonIgnore]
    [BindNever]
    public Guid TableId { get; init; }
}
