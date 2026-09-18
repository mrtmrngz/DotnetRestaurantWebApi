using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;

namespace RestaurantApi.Application.Features.Category.Events.UpdateCategoryEvent;

public class UpdateCategoryEventHandler : INotificationHandler<UpdateCategoryEvent>
{
    private readonly ILogger<UpdateCategoryEventHandler> _logger;
    private readonly IFileStorage _fileStorage;

    public UpdateCategoryEventHandler(ILogger<UpdateCategoryEventHandler> logger, IFileStorage fileStorage)
    {
        _logger = logger;
        _fileStorage = fileStorage;
    }

    public async Task Handle(UpdateCategoryEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Eski kategori resmi siliniyor: CategoryId: {cId}, PublicId:{pId}",
                notification.CategoryId, notification.OldPublicId);
            
            await _fileStorage.DeleteAsync(notification.OldPublicId);
            
            _logger.LogInformation("Eski kategori resmi başarılı bir şekilde silindi: CategoryId: {cId}",
                notification.CategoryId);
        }
        catch (Exception deleteEx)
        {
            _logger.LogCritical(deleteEx,
                "KRİTİK HATA: Veritabanı veya işlem hatası sonrası yüklenen dosya depolama servisinden SİLİNEMEDİ! Yetim dosya kaldı. PublicId: {PublicId}",
                notification.OldPublicId);
        }
    }
}