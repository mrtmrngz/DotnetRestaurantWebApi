using AutoMapper;
using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Profiles;

public class MediaMappingProfile: Profile
{
    public MediaMappingProfile()
    {
        CreateMap<UploadFileResult, Media>();
    }
}