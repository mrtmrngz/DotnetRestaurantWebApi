using RestaurantApi.Application.Models.Dtos.AddressDtos;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface IAddressService
{
    Task<BaseResponse> CreateAddressAsyncService(Address dto, CancellationToken ctx);
}