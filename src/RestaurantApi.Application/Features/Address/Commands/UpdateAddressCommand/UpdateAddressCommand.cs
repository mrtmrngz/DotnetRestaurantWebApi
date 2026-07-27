using System.Text.Json.Serialization;
using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Commands.UpdateAddressCommand;

public record UpdateAddressCommand(
    string? Title,
    string? RecipientName,
    string? City,
    string? Town,
    string? Neighborhood,
    string? Street,
    string? BuildingInfo,
    string? BuildingNumber,
    string? PhoneNumber,
    bool? IsDefault,
    string? ZipCode
) : IRequest<BaseResponse>
{
    [JsonIgnore] 
    public Guid UserId { get; init; }
    
    [JsonIgnore] 
    public Guid AddressId { get; init; }
};