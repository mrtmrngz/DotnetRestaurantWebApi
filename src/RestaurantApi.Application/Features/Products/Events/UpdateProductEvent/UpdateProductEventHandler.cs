using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;

namespace RestaurantApi.Application.Features.Products.Events.UpdateProductEvent;

public class UpdateProductEventHandler: INotificationHandler<UpdateProductEvent>
{
    private readonly ILogger<UpdateProductEventHandler> _logger;
    private readonly IFileStorage _fileStorage;

    public UpdateProductEventHandler(ILogger<UpdateProductEventHandler> logger, IFileStorage fileStorage)
    {
        _logger = logger;
        _fileStorage = fileStorage;
    }

    public async Task Handle(UpdateProductEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Eski ürün fotoğrafları siliniyor");

        try
        {
            await _fileStorage.SafeDeleteMultipleFilesAsync(notification.PublicIDs);
            
            _logger.LogInformation("Eski ürün fotoğrafları başarılı bir şekilde silindi.");
        }
        catch
        {
            foreach (var pbs in notification.PublicIDs)
            {
                _logger.LogInformation("Yetim dosya s3 içerisinde kaldı. PublicId:{id}", pbs);
            }
            throw;
        }
    }
}