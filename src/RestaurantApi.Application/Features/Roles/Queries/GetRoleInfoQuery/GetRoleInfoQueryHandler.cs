using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;

public class GetRoleInfoQueryHandler
    : IRequestHandler<GetRoleInfoQuery, GeneralSuccessResponseWithData<GetRoleInfoQueryResult>>
{
    private readonly ILogger<GetRoleInfoQueryHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly RoleRules _roleRules;
    private readonly IMapper _mapper;

    public GetRoleInfoQueryHandler(ILogger<GetRoleInfoQueryHandler> logger, IRoleRepository roleRepository,
        RoleRules roleRules, IMapper mapper)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _roleRules = roleRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<GetRoleInfoQueryResult>> Handle(GetRoleInfoQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rol bilgisi getirme işlemi başlıyor. RoleId:{RoleId}", request.RoleId);

        AppRole? role = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);

        _roleRules.ShouldRoleExist(role, request.RoleId);

        GetRoleInfoQueryResult roleInfo = _mapper.Map<GetRoleInfoQueryResult>(role!);

        _logger.LogInformation("Rol bilgisi getirildi. RoleId:{RoleId}, RoleName:{RoleName}, IsSystemRole:{IsSystemRole}",
            roleInfo.Id, roleInfo.Name, roleInfo.IsSystemRole);

        return new GeneralSuccessResponseWithData<GetRoleInfoQueryResult>(data: roleInfo);
    }
}
