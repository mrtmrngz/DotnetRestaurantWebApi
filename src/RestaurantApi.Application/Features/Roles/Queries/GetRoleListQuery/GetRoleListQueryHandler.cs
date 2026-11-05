using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;

public class GetRoleListQueryHandler
    : IRequestHandler<GetRoleListQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>>
{
    private readonly ILogger<GetRoleListQueryHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;

    public GetRoleListQueryHandler(ILogger<GetRoleListQueryHandler> logger, IRoleRepository roleRepository,
        IMapper mapper)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>> Handle(
        GetRoleListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rol listeleme işlemi başlıyor.");

        List<GetRoleListQueryResult> roles = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ProjectTo<GetRoleListQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        // (2) role bağlı kullanıcı sayıları join tablosundan gruplanarak alınır ve satırlara işlenir
        Dictionary<Guid, int> userCounts = await _roleRepository
            .GetUserCountsByRoleIdAsync(cancellationToken);

        foreach (GetRoleListQueryResult role in roles)
        {
            role.UserCount = userCounts.GetValueOrDefault(role.Id);
        }

        _logger.LogInformation("Rol listesi hazırlandı. RoleCount:{RoleCount}", roles.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>(data: roles);
    }
}
