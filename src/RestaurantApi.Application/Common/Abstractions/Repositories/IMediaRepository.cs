using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IMediaRepository
{
    void CreateMedia(Media media, CancellationToken ctx);
    void AddRange(List<Media> medias, CancellationToken ctx);
    Task<Media?> GetMedia(Guid? mediaId, CancellationToken ctx);
    void DeleteMultipleMedia(IList<Media> medias);
    IQueryable<Media> GetAllAsQueryable();
}