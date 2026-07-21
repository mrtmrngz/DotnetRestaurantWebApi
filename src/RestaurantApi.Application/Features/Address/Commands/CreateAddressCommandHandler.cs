using AutoMapper;
using MediatR;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Commands;

public class CreateAddressCommandHandler: IRequestHandler<CreateAddressCommand, BaseResponse>
{
    private readonly IAddressService _addressService;
    private readonly IMapper _mapper;

    public CreateAddressCommandHandler(IAddressService addressService, IMapper mapper)
    {
        _addressService = addressService;
        _mapper = mapper;
    }

    public async Task<BaseResponse> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        var data = _mapper.Map<Domain.Entities.Address>(request);
        return await _addressService.CreateAddressAsyncService(data, cancellationToken);
    }
}