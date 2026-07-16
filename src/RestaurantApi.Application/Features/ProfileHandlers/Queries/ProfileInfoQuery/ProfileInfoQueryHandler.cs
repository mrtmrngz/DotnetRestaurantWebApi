using AutoMapper;
using MediatR;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;

public class
    ProfileInfoQueryHandler : IRequestHandler<ProfileInfoQuery, GeneralSuccessResponseWithData<ProfileInfoQueryResult>>
{

    private readonly UserRules _userRules;
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public ProfileInfoQueryHandler(UserRules userRules, ICacheService cacheService, IUserRepository userRepository, IMapper mapper)
    {
        _userRules = userRules;
        _cacheService = cacheService;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<ProfileInfoQueryResult>> Handle(ProfileInfoQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ProfileInfoKey(request.UserId.ToString());

        var user = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            var rawUser = await _userRepository.GetByIdAsync(request.UserId);
            await _userRules.UserShouldExist404(rawUser);
            var mappedUser = _mapper.Map<ProfileInfoQueryResult>(rawUser);

            return mappedUser;
        }, TimeSpan.FromHours(1));

        return new GeneralSuccessResponseWithData<ProfileInfoQueryResult>(data: user, code: Codes.FETH_DATA_SUCCESS);
    }
}