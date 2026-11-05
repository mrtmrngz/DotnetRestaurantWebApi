using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Roles.Commands;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.RoleExamples;
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

    /// <summary>
    /// Bir role bağlı izinleri listeler.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `roleId` **route'tan** alınır; istek gövdesi/query string kabul edilmez.
    /// 2. Rol bulunamazsa **404** döner; izni olmayan rol bir hata değildir (**200 + boş liste**).
    /// 3. Her satırda iznin `id`, `name` ve `description` alanları döner; liste `name` alanına göre alfabetiktir.
    /// 4. Sistem rolleri de okunabilir (ör. ADMIN tüm izinlere sahiptir); kısıt yalnızca izinlerin
    ///    **güncellenmesinde** uygulanır, bu uç salt okunurdur.
    /// Uç rol izinlerini yöneten ekranı besler ve `role:view` izniyle korunur.
    /// </remarks>
    /// <param name="roleId">İzinleri listelenecek rolün benzersiz id'si.</param>
    /// <response code="200">Role bağlı izinler alfabetik olarak döner (boş liste olabilir).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:view` izni bulunmadığında.</response>
    /// <response code="404">Rol bulunamadığında ya da route'taki id geçerli bir GUID olmadığında.</response>
    [HttpGet("{roleId:guid}/permissions")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetRolePermissionsResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.View)]
    public async Task<IActionResult> GetRolePermissions([FromRoute] Guid roleId)
    {
        var query = new GetRolePermissionsQuery
        {
            RoleId = roleId
        };

        GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Bir rolün izin kümesini istekteki izinlerle değiştirir.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `roleId` **route'tan**, izin listesi **gövdeden** (`permissionIds`) alınır; gövdedeki rol id dikkate alınmaz.
    /// 2. Gövde, rolün **yeni izin kümesinin tamamıdır**: istekte olmayan izinler kaldırılır, eksik izinler eklenir.
    ///    Boş liste gönderilirse rolün tüm izinleri kaldırılır (rol silinmez).
    /// 3. Rol bulunamazsa **404**; istekteki izinlerden biri bile bulunamazsa hiçbir değişiklik yazılmaz ve **404** döner.
    /// 4. Sistem rolleri (`ADMIN` / `USER`) güncellenemez (**422**).
    /// 5. Değişiklik yoksa yazma yapılmaz; uç **idempotenttir** ve yine **200** döner.
    /// 6. Ekleme ve silme tek `SaveChanges` ile atomik yazılır ve rolün izin cache'i (`role:{rol}:permissions`)
    ///    temizlenir; temizlenmezse yetki kararı cache süresi boyunca eski kalırdı.
    /// </remarks>
    /// <param name="roleId">İzinleri güncellenecek rolün benzersiz id'si.</param>
    /// <param name="command">Gövde: `permissionIds` (zorunlu; rolün yeni izin kümesinin tamamı).</param>
    /// <response code="200">Rol izinleri güncellendi; `CONTENT_UPDATED_SUCCESS` kodu döner.</response>
    /// <response code="400">Doğrulama hataları (izin listesi yok, tekrar eden ya da boş izin id'si).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:update` izni bulunmadığında.</response>
    /// <response code="404">Rol bulunamadığında ya da istekteki izinlerden biri bulunamadığında.</response>
    /// <response code="422">Sistem rolleri (`ADMIN` / `USER`) güncellenmek istendiğinde.</response>
    [HttpPut("{roleId:guid}/permissions")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    [SwaggerRequestExample(typeof(UpdateRolePermissionCommand), typeof(UpdateRolePermissionRequestExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.Update)]
    public async Task<IActionResult> UpdateRolePermissions([FromRoute] Guid roleId,
        [FromBody] UpdateRolePermissionCommand command)
    {
        var commandToSend = command with { RoleId = roleId };

        BaseResponse response = await _mediator.Send(commandToSend);

        return Ok(response);
    }
}
