using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Rules.MediaRules;

public class MediaRules
{
    private readonly ILogger<MediaRules> _logger;

    public MediaRules(ILogger<MediaRules> logger)
    {
        _logger = logger;
    }

    public Task ShouldMediaExist(Media? media)
    {
        if (media is null)
        {
            throw new NotFoundException("İlgili medya bulunamadı.");
        }

        return Task.CompletedTask;
    }
}