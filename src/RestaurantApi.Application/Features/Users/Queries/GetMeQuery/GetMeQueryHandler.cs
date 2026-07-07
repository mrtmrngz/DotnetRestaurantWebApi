using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Queries.GetMeQuery;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, GeneralSuccessResponseWithData<GetMeQueryResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetMeQueryHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly UserRules _userRules;

    public GetMeQueryHandler(IUserRepository userRepository, ILogger<GetMeQueryHandler> logger, ICacheService cacheService, IMapper mapper, UserRules userRules)
    {
        _userRepository = userRepository;
        _logger = logger;
        _cacheService = cacheService;
        _mapper = mapper;
        _userRules = userRules;
    }

    public async Task<GeneralSuccessResponseWithData<GetMeQueryResult>> Handle(GetMeQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.GetMeKey(request.UserId);
        var user = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            var rawUser = await _userRepository.GetByIdAsync(Guid.Parse(request.UserId));
            await _userRules.UserShouldExist404(rawUser);
            var mappingUser = _mapper.Map<GetMeQueryResult>(rawUser);

            return mappingUser;
        }, TimeSpan.FromHours(1));

        return new GeneralSuccessResponseWithData<GetMeQueryResult>(data: user, code: Codes.FETH_DATA_SUCCESS);
    }
}