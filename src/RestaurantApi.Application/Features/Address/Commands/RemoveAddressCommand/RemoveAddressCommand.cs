using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Commands.RemoveAddressCommand;

public record RemoveAddressCommand : IRequest<BaseResponse>
{
    [JsonIgnore]
    public Guid AddressId { get; init; }
    [JsonIgnore]
    public Guid UserId { get; init; }
};