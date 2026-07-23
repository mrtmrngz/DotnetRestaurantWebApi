using AutoMapper;
using RestaurantApi.Application.Features.Address.Commands;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;

namespace RestaurantApi.Application.Features.Profiles;

public class AddressMappingProfile: Profile
{
    public AddressMappingProfile()
    {
        CreateMap<CreateAddressCommand, Domain.Entities.Address>();
        CreateMap<Domain.Entities.Address, GetUserAddressQueryResult>();
    }
}