using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Roles.Commands;

public class CreateRoleCommandHandler: IRequestHandler<CreateRoleCommand, BaseResponse>
{
    private readonly ILogger<CreateRoleCommandHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly RoleRules _roleRules;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(ILogger<CreateRoleCommandHandler> logger, IRoleRepository roleRepository,
        RoleRules roleRules, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _roleRules = roleRules;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        string roleName = request.Name.Trim();
        string normalizedName = roleName.ToUpperInvariant();

        _logger.LogInformation("Rol oluşturma işlemi başlıyor. RoleName:{RoleName}", roleName);

        await EnsureRoleNameIsAvailableAsync(roleName, normalizedName, cancellationToken);

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = normalizedName,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsSystemRole = false
        };

        _roleRepository.CreateRole(role, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rol oluşturuldu. RoleId:{RoleId}, RoleName:{RoleName}", role.Id, role.Name);

        return new BaseResponse
        {
            Message = $"'{roleName}' rolü başarılı bir şekilde oluşturuldu.",
            Code = Codes.CONTENT_CREATED_SUCCESS
        };
    }

    #region Helper Methods

    private async Task EnsureRoleNameIsAvailableAsync(string roleName, string normalizedName, CancellationToken ctx)
    {
        bool roleNameAlreadyExists = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Name == roleName || x.NormalizedName == normalizedName, ctx);

        _roleRules.ShouldRoleNameBeAvailable(roleNameAlreadyExists, roleId: null, roleName);
    }

    #endregion
}
