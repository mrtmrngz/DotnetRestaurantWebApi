using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;

public class GetUserOrdersQueryHandler
    : IRequestHandler<GetUserOrdersQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>>
{
    private readonly ILogger<GetUserOrdersQueryHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IMapper _mapper;

    public GetUserOrdersQueryHandler(ILogger<GetUserOrdersQueryHandler> logger, IOrderRepository orderRepository,
        IUserRepository userRepository, UserRules userRules, IMapper mapper)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _userRules = userRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>> Handle(
        GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcı siparişleri listeleme işlemi başlıyor. UserId:{UserId}", request.UserId);

        await ValidateUserExistsAsync(request.UserId, cancellationToken);

        IReadOnlyList<GetUserOrdersQueryResult> orders = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(x => x.UserId == request.UserId)
            .OrderByDescending(x => x.OrderDate)
            .ProjectTo<GetUserOrdersQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            _logger.LogInformation("Kullanıcının siparişi bulunamadı. UserId:{UserId}", request.UserId);

            return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>(data: orders);
        }

        _logger.LogInformation("Kullanıcı siparişleri listelendi. UserId:{UserId}, OrderCount:{OrderCount}",
            request.UserId, orders.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>(data: orders);
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
