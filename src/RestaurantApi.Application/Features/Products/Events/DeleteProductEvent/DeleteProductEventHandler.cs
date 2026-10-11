using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;

namespace RestaurantApi.Application.Features.Products.Events.DeleteProductEvent;

public class DeleteProductEventHandler: INotificationHandler<DeleteProductEvent>
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteProductEventHandler> _logger;

    public DeleteProductEventHandler(IDiscountRepository discountRepository, IUnitOfWork unitOfWork,
        ICacheService cacheService, ILogger<DeleteProductEventHandler> logger)
    {
        _discountRepository = discountRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(DeleteProductEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ürün silindi, ilişkili indirim kontrol ediliyor. ProductId:{PrdId}", notification.ProductId);

        var discount = await _discountRepository.GetAllAsQueryable()
            .FirstOrDefaultAsync(d => d.ProductId == notification.ProductId, cancellationToken);

        if (discount is null)
        {
            _logger.LogInformation("Silinen ürün için indirim bulunmadı. ProductId:{PrdId}", notification.ProductId);
            return;
        }

        discount.IsActive = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ürün silindiği için indirim pasife alındı. DiscountId:{DiscountId}, ProductId:{PrdId}",
            discount.Id, notification.ProductId);

        await _cacheService.RemoveAsync(CacheKeys.AdminDiscounts());
    }
}
