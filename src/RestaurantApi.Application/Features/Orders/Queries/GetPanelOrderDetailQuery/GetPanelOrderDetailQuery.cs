using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;

// Panel akışı: tek bir siparişin (masa ya da kullanıcı siparişi) detayını getiren sorgu.
// OrderId route'tan gelir ve istemci gövdesinden kabul edilmez.
public record GetPanelOrderDetailQuery: IRequest<GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>>
{
    [JsonIgnore]
    [BindNever]
    public Guid OrderId { get; init; }
}
