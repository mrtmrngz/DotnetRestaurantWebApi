using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class MediaRepository: IMediaRepository
{
    private readonly ApiContext _context;

    public MediaRepository(ApiContext context)
    {
        _context = context;
    }

    public void CreateMedia(Media media, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.Media.Add(media);
    }

    public async Task<Media?> GetMedia(Guid? mediaId, CancellationToken ctx)
    {
        return await _context.Media.FirstOrDefaultAsync(m => m.Id == mediaId, ctx);
    }
}