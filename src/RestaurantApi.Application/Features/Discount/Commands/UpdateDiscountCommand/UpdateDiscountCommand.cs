using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Discount.Commands.UpdateDiscountCommand;

public record UpdateDiscountCommand(
    double? Rate = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool? IsActive = null,
    Guid? ProductId = null
) : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid DiscountId { get; init; }
}
