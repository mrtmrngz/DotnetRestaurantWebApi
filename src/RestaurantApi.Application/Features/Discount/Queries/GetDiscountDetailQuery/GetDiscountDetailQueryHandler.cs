using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.DiscountRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;

public class GetDiscountDetailQueryHandler: IRequestHandler<GetDiscountDetailQuery, GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>
{
    private readonly ILogger<GetDiscountDetailQueryHandler> _logger;
    private readonly IDiscountRepository _discountRepository;
    private readonly DiscountRules _discountRules;
    private readonly IMapper _mapper;

    public GetDiscountDetailQueryHandler(ILogger<GetDiscountDetailQueryHandler> logger,
        IDiscountRepository discountRepository, DiscountRules discountRules, IMapper mapper)
    {
        _logger = logger;
        _discountRepository = discountRepository;
        _discountRules = discountRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>> Handle(
        GetDiscountDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("İndirim detayı getiriliyor... DiscountId:{DiscountId}", request.Id);

        var result = await _discountRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ProjectTo<GetDiscountDetailQueryResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        _discountRules.ShouldDiscountExist(result, request.Id);

        result!.Status = _discountRules.ResolveStatus(result.StartDate, result.EndDate, DateTime.UtcNow);

        _logger.LogInformation("İndirim detayı bulundu. DiscountId:{DiscountId}, Status:{Status}",
            request.Id, result.Status);

        return new GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>(data: result);
    }
}
