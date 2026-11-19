using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Commands.UpdateUserRoleCommand;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, BaseResponse>
{
    private readonly ILogger<UpdateUserRoleCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IRoleRepository _roleRepository;
    private readonly RoleRules _roleRules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public UpdateUserRoleCommandHandler(ILogger<UpdateUserRoleCommandHandler> logger, IUserRepository userRepository,
        UserRules userRules, IRoleRepository roleRepository, RoleRules roleRules, IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _userRules = userRules;
        _roleRepository = roleRepository;
        _roleRules = roleRules;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        HashSet<Guid> requestedIds = request.RoleIds.Select(id => Guid.Parse(id)).ToHashSet();

        _logger.LogInformation(
            "Kullanıcı rol güncelleme işlemi başlıyor. UserId:{UserId}, RequestedRoleCount:{RequestedRoleCount}",
            request.UserId, requestedIds.Count);

        await FindAndValidateUserExist(request.UserId, cancellationToken);

        await EnsureRolesExist(requestedIds, cancellationToken);

        var (toAdd, toRemove) = await FindChanges(requestedIds, request.UserId, cancellationToken);

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            _logger.LogInformation(
                "Kullanıcının rolleri istekteki rollerle aynı; güncelleme uygulanmadı. UserId:{UserId}, RoleCount:{RoleCount}",
                request.UserId, requestedIds.Count);
        }
        else
        {
            if (toRemove.Count > 0)
            {
                _logger.LogInformation("Kullanıcıdan roller kaldırılıyor. UserId:{UserId}, RemoveCount:{RemoveCount}",
                    request.UserId, toRemove.Count);

                _userRepository.UserRolesRemoveRange(toRemove);
            }

            if (toAdd.Count > 0)
            {
                _logger.LogInformation("Kullanıcıya roller ekleniyor. UserId:{UserId}, AddCount:{AddCount}",
                    request.UserId, toAdd.Count);

                _userRepository.UserRolesAddRange(toAdd);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateCache(request.UserId);

            _logger.LogInformation(
                "Kullanıcı rolleri güncellendi. UserId:{UserId}, AddedCount:{AddedCount}, RemovedCount:{RemovedCount}, RoleCount:{RoleCount}",
                request.UserId, toAdd.Count, toRemove.Count, requestedIds.Count);
        }

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = "Kullanıcının rolleri başarılı bir şekilde güncellendi."
        };
    }

    #region Helper Methods

    private async Task FindAndValidateUserExist(Guid userId, CancellationToken ctx)
    {
        _logger.LogInformation("Güncellenecek kullanıcı aranıyor. UserId:{UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);

        await _userRules.UserShouldExist404(user);

        _logger.LogInformation("Kullanıcı doğrulandı. UserId:{UserId}, IsSystemUser:{IsSystemUser}",
            userId, user!.IsSystemUser);

        _userRules.ShouldUserRolesUpdatable(user.IsSystemUser, userId);
    }

    private async Task EnsureRolesExist(HashSet<Guid> ids, CancellationToken ctx)
    {
        IReadOnlyCollection<Guid> roles = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ctx);

        _roleRules.ShouldRolesExist(roles, ids);

        _logger.LogInformation("İstekteki roller doğrulandı. RoleCount:{RoleCount}", roles.Count);
    }

    private async Task<(List<IdentityUserRole<Guid>> ToAdd, List<IdentityUserRole<Guid>> ToRemove)> FindChanges(
        HashSet<Guid> requestedIds, Guid userId, CancellationToken ctx
        )
    {
        IReadOnlyCollection<IdentityUserRole<Guid>> currentRoles =
            await _userRepository.GetUserRoles(userId, ctx);
        
        HashSet<Guid> currentRolesId = currentRoles.Select(cr => cr.RoleId).ToHashSet();

        _logger.LogInformation("Kullanıcının mevcut rolleri getirildi. UserId:{UserId}, CurrentRoleCount:{Count}",
            userId, currentRolesId.Count);

        List<IdentityUserRole<Guid>> toAdd = requestedIds.Except(currentRolesId)
            .Select(ri => new IdentityUserRole<Guid>
            {
                RoleId = ri,
                UserId = userId
            })
            .ToList();

        List<IdentityUserRole<Guid>> toRemove = currentRoles
            .Where(cr => !requestedIds.Contains(cr.RoleId))
            .ToList();

        return (toAdd, toRemove);
    }

    private async Task InvalidateCache(Guid userId)
    {
        string cacheKey = CacheKeys.UserRoles(userId.ToString());

        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation("Kullanıcı rol cache'i temizlendi. UserId:{UserId}, CacheKey:{CacheKey}",
            userId, cacheKey);
    }

    #endregion
}