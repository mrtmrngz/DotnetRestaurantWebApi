using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;

public class GetPanelOrderDetailQueryHandler
    : IRequestHandler<GetPanelOrderDetailQuery, GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>>
{
    private readonly ILogger<GetPanelOrderDetailQueryHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetPanelOrderDetailQueryHandler(ILogger<GetPanelOrderDetailQueryHandler> logger,
        IOrderRepository orderRepository, IMapper mapper)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>> Handle(
        GetPanelOrderDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Panel sipariş detayı getirme işlemi başlıyor. OrderId:{OrderId}", request.OrderId);

        // Sipariş tek sorguda okunur: haritalamanın tamamı OrderProfile'da tanımlandığı için masa numarası ve
        // müşteri adı da aynı projeksiyonda (masa ve kullanıcı tablolarına yapılan sol birleşimle) gelir.
        GetPanelOrderDetailQueryResult? orderDetail = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(x => x.Id == request.OrderId)
            .ProjectTo<GetPanelOrderDetailQueryResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderDetail is null)
        {
            _logger.LogWarning("Panelde gösterilecek sipariş bulunamadı. OrderId:{OrderId}", request.OrderId);

            throw new NotFoundException("Sipariş bulunamadı.");
        }

        _logger.LogInformation(
            "Panel sipariş detayı getirildi. OrderId:{OrderId}, TableNumber:{TableNumber}, CustomerFullName:{CustomerFullName}, ItemCount:{ItemCount}",
            orderDetail.OrderId, orderDetail.TableNumber, orderDetail.CustomerFullName, orderDetail.OrderItems.Count);

        return new GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>(data: orderDetail);
    }
}
