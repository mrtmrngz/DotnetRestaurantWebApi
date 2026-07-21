using AutoMapper;
using RestaurantApi.Application.Features.Address.Commands;

namespace RestaurantApi.Application.Features.Profiles;

public class AddressMappingProfile: Profile
{
    public AddressMappingProfile()
    {
        CreateMap<CreateAddressCommand, Domain.Entities.Address>();
    }
}