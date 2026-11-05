using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;

public class GetRolePermissionsQueryHandler
    : IRequestHandler<GetRolePermissionsQuery,
        GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>>
{
    private readonly ILogger<GetRolePermissionsQueryHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly RoleRules _roleRules;
    private readonly IMapper _mapper;

    public GetRolePermissionsQueryHandler(ILogger<GetRolePermissionsQueryHandler> logger,
        IRoleRepository roleRepository, IRolePermissionRepository rolePermissionRepository, RoleRules roleRules,
        IMapper mapper)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleRules = roleRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>> Handle(
        GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rol izinleri listeleme işlemi başlıyor. RoleId:{RoleId}", request.RoleId);

        // (1) rol yoksa 404; izni olmayan rol bir hata değil, boş liste döner (kural RoleRules içindedir)
        await ValidateRoleExistsAsync(request.RoleId, cancellationToken);

        // (2) role bağlı izinler tek sorguda, izin adına göre sıralı ve doğrudan projeksiyonla alınır
        IReadOnlyList<GetRolePermissionsQueryResult> permissions = await _rolePermissionRepository
            .GetAllAsQueryable()
            .AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == request.RoleId)
            .Select(rolePermission => rolePermission.Permission)
            .OrderBy(permission => permission.Name)
            .ProjectTo<GetRolePermissionsQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Rol izinleri listelendi. RoleId:{RoleId}, PermissionCount:{PermissionCount}",
            request.RoleId, permissions.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>(data: permissions);
    }

    #region Helper Methods

    private async Task ValidateRoleExistsAsync(Guid roleId, CancellationToken ctx)
    {
        _logger.LogInformation("İzinleri listelenecek rol aranıyor. RoleId:{RoleId}", roleId);

        AppRole? role = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, ctx);

        _roleRules.ShouldRoleExist(role, roleId);
    }

    #endregion
}
