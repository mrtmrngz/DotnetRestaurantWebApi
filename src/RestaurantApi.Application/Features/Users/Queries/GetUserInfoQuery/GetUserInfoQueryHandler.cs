using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;

public class GetUserInfoQueryHandler
    : IRequestHandler<GetUserInfoQuery, GeneralSuccessResponseWithData<GetUserInfoQueryResult>>
{
    private readonly ILogger<GetUserInfoQueryHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IMapper _mapper;

    public GetUserInfoQueryHandler(ILogger<GetUserInfoQueryHandler> logger, IUserRepository userRepository,
        UserRules userRules, IMapper mapper)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userRules = userRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetUserInfoQueryResult>> Handle(GetUserInfoQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcı bilgisi getirme işlemi başlıyor. UserId:{UserId}", request.UserId);

        AppUser? user = await _userRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        await _userRules.UserShouldExist404(user);

        GetUserInfoQueryResult userInfo = _mapper.Map<GetUserInfoQueryResult>(user!);

        Dictionary<Guid, List<string>> roleNames = await _userRepository
            .GetRoleNamesByUserIdsAsync(new List<Guid> { request.UserId }, cancellationToken);

        userInfo.Roles = roleNames.GetValueOrDefault(request.UserId) ?? new List<string>();

        _logger.LogInformation("Kullanıcı bilgisi getirildi. UserId:{UserId}, RoleCount:{RoleCount}",
            userInfo.Id, userInfo.Roles.Count);

        return new GeneralSuccessResponseWithData<GetUserInfoQueryResult>(data: userInfo);
    }
}
