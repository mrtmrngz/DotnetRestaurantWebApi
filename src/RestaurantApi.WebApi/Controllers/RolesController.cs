using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sistemdeki tüm rolleri kullanıcı ve izin sayılarıyla listeler.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. İstek parametre almaz; roller `Name` alanına göre alfabetik döner.
    /// 2. Her satırda `id`, `name`, `userCount`, `permissionCount` ve `isSystemRole` döner.
    /// 3. `userCount` role bağlı kullanıcı sayısıdır (AspNetUserRoles join tablosundan sayılır),
    ///    `permissionCount` role bağlı izin sayısıdır (role_permissions kayıtları).
    /// 4. `isSystemRole`, rol `AppRoles` içindeki statik rollerden (`ADMIN` / `USER`) biri ise **true**,
    ///    sonradan oluşturulan roller için **false** döner.
    /// Liste tek sorguda, doğrudan projeksiyonla üretilir; rol yoksa **200 + boş liste** döner.
    /// </remarks>
    /// <response code="200">Roller alfabetik olarak döner (boş liste olabilir).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:view` izni bulunmadığında.</response>
    [HttpGet]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetRoleListResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.View)]
    public async Task<IActionResult> GetRoleList()
    {
        var query = new GetRoleListQuery();

        GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }
}
