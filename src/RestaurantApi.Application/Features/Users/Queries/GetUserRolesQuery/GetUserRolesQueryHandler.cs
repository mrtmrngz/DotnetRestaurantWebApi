using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;

public class GetUserRolesQueryHandler
    : IRequestHandler<GetUserRolesQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>>
{
    private readonly ILogger<GetUserRolesQueryHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly UserRules _userRules;
    private readonly IMapper _mapper;

    public GetUserRolesQueryHandler(ILogger<GetUserRolesQueryHandler> logger, IUserRepository userRepository,
        IRoleRepository roleRepository, UserRules userRules, IMapper mapper)
    {
        _logger = logger;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRules = userRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>> Handle(
        GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcı rol durumu listeleme işlemi başlıyor. UserId:{UserId}", request.UserId);

        AppUser? user = await _userRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        await _userRules.UserShouldExist404(user);

        Dictionary<Guid, List<string>> roleNames = await _userRepository
            .GetRoleNamesByUserIdsAsync(new List<Guid> { request.UserId }, cancellationToken);

        List<string> userRoleNames = roleNames.GetValueOrDefault(request.UserId) ?? new List<string>();

        List<GetUserRolesQueryResult> roles = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ProjectTo<GetUserRolesQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        foreach (GetUserRolesQueryResult role in roles)
        {
            role.HasRole = userRoleNames.Contains(role.Name);
        }

        _logger.LogInformation(
            "Kullanıcı rol durumu listesi hazırlandı. UserId:{UserId}, RoleCount:{RoleCount}, AssignedRoleCount:{AssignedRoleCount}",
            request.UserId, roles.Count, userRoleNames.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>(data: roles);
    }
}
