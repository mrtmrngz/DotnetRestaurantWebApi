using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IMediaRepository
{
    void CreateMedia(Media media, CancellationToken ctx);
    Task<Media?> GetMedia(Guid? mediaId, CancellationToken ctx);
}