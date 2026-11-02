using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;

public class GetUserOrderDetailQueryHandler
    : IRequestHandler<GetUserOrderDetailQuery, GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>>
{
    private readonly ILogger<GetUserOrderDetailQueryHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IMapper _mapper;

    public GetUserOrderDetailQueryHandler(ILogger<GetUserOrderDetailQueryHandler> logger,
        IOrderRepository orderRepository, IUserRepository userRepository, UserRules userRules, IMapper mapper)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _userRules = userRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>> Handle(
        GetUserOrderDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcı sipariş detayı getirme işlemi başlıyor. OrderId:{OrderId}, UserId:{UserId}",
            request.OrderId, request.UserId);

        // (1) token geçerli olsa bile kullanıcı kaydı bulunamazsa 404 döner
        await ValidateUserExistsAsync(request.UserId, cancellationToken);

        // (2) detay tek sorguda okunur; IDOR kontrolü sorgunun kendisindedir
        GetUserOrderDetailQueryResult? orderDetail = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(x => x.Id == request.OrderId && x.UserId == request.UserId)
            .ProjectTo<GetUserOrderDetailQueryResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        // (3) sipariş yoksa (ya da başka bir kullanıcıya aitse) 404 döner
        if (orderDetail is null)
        {
            _logger.LogWarning(
                "Sipariş bulunamadı ya da isteği yapan kullanıcıya ait değil. OrderId:{OrderId}, UserId:{UserId}",
                request.OrderId, request.UserId);

            throw new NotFoundException("Sipariş bulunamadı.");
        }

        _logger.LogInformation("Sipariş detayı getirildi. OrderId:{OrderId}, UserId:{UserId}, ItemCount:{ItemCount}",
            orderDetail.OrderId, request.UserId, orderDetail.OrderItems.Count);

        return new GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>(data: orderDetail);
    }

    #region Helper Methods

    private async Task ValidateUserExistsAsync(Guid userId, CancellationToken ctx)
    {
        bool userExists = await _userRepository.AnyUserExistAsync(userId, ctx);

        await _userRules.ShouldUserExistBool404(userExists);

        _logger.LogInformation("Kullanıcı doğrulandı. UserId:{UserId}", userId);
    }

    #endregion
}
