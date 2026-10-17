using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;

public class GetRestaurantTableDetailQueryHandler: IRequestHandler<GetRestaurantTableDetailQuery, GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>>
{
    private readonly ILogger<GetRestaurantTableDetailQueryHandler> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly IMapper _mapper;

    public GetRestaurantTableDetailQueryHandler(ILogger<GetRestaurantTableDetailQueryHandler> logger,
        IRestaurantTableRepository restaurantTableRepository, RestaurantTableRules restaurantTableRules,
        IMapper mapper)
    {
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _restaurantTableRules = restaurantTableRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>> Handle(
        GetRestaurantTableDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa detayı getiriliyor... TableId:{TableId}", request.Id);

        var table = await _restaurantTableRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        _restaurantTableRules.ShouldTableExist(table, request.Id);

        var result = _mapper.Map<GetRestaurantTableDetailQueryResult>(table!);

        _logger.LogInformation(
            "Masa detayı bulundu. TableId:{TableId}, TableNumber:{TableNumber}, IsActive:{IsActive}",
            result.Id, result.Number, result.IsActive);

        return new GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>(data: result);
    }
}
