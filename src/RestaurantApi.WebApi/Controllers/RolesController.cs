using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Roles.Commands;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;
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

    /// <summary>
    /// Bir rolün adını günceller.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `roleId` **route'tan**, yeni ad **gövdeden** (`name`) alınır; gövdedeki rol id dikkate alınmaz.
    /// 2. Rol bulunamazsa **404**; rol **sistem rolü** ise (`IsSystemRole`) **422** döner.
    /// 3. Gönderilen ad (ve normalize edilmiş hali) rolün mevcut adıyla aynıysa yazma yapılmaz;
    ///    uç **idempotenttir** ve yine **200** döner.
    /// 4. Gönderilen ad ya da normalize edilmiş hali **başka bir role** aitse **409** döner.
    /// 5. Güncelleme tek `UPDATE` ifadesiyle yazılır; bu sırada rol değişmiş ya da silinmişse
    ///    (hiçbir satır etkilenmez) **409** döner.
    /// 6. Başarılı güncellemeden sonra rol adına bağlı cache anahtarları temizlenir: eski ve yeni adın
    ///    izin listesi (`role:{rol}:permissions`) ile rolün kullanıcılarının rol anahtarları
    ///    (`roles:{userId}`, token üretiminde 7 gün kullanılır).
    /// </remarks>
    /// <param name="roleId">Adı güncellenecek rolün benzersiz id'si.</param>
    /// <param name="command">Gövde: `name` (zorunlu, en fazla 256 karakter).</param>
    /// <response code="200">Rol bilgisi güncellendi; `CONTENT_UPDATED_SUCCESS` kodu döner.</response>
    /// <response code="400">Doğrulama hataları (rol adı yok ya da 256 karakterden uzun).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:update` izni bulunmadığında.</response>
    /// <response code="404">Rol bulunamadığında.</response>
    /// <response code="409">Rol adı başka bir rol tarafından kullanılıyorsa ya da güncelleme eşzamanlı bir değişiklik nedeniyle uygulanamazsa.</response>
    /// <response code="422">Sistem rolleri (`ADMIN` / `USER`) güncellenmek istendiğinde.</response>
    [HttpPatch("{roleId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    [SwaggerRequestExample(typeof(UpdateRoleInfoCommand), typeof(UpdateRoleInfoRequestExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.Update)]
    public async Task<IActionResult> UpdateRoleInfo([FromRoute] Guid roleId,
        [FromBody] UpdateRoleInfoCommand command)
    {
        var commandToSend = command with { RoleId = roleId };

        BaseResponse response = await _mediator.Send(commandToSend);

        return Ok(response);
    }

    /// <summary>
    /// Yeni bir rol oluşturur.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. Rol adı **gövdeden** (`name`) alınır; istek başka parametre almaz ve ad en fazla 256 karakter olabilir.
    /// 2. Aynı adı (ya da normalize edilmiş halini) taşıyan bir rol varsa **409** döner.
    /// 3. Rol, **sistem rolü olmayan** bir uygulama rolü olarak (`IsSystemRole = false`) eklenir; sistem
    ///    rolleri yalnızca seed ile oluşturulur.
    /// 4. Oluşturulan rolün izni yoktur; izinler `PUT api/roles/{roleId}/permissions` ile atanır.
    /// Rol listesi cache'lenmediği ve yeni rolün izin kaydı olmadığı için temizlenecek cache anahtarı yoktur.
    /// </remarks>
    /// <param name="command">Gövde: `name` (zorunlu, en fazla 256 karakter).</param>
    /// <response code="201">Rol oluşturuldu; `CONTENT_CREATED_SUCCESS` kodu döner.</response>
    /// <response code="400">Doğrulama hataları (rol adı yok, boş ya da 256 karakterden uzun).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:create` izni bulunmadığında.</response>
    /// <response code="409">Rol adı (ya da normalize edilmiş hali) başka bir rol tarafından kullanılıyorsa.</response>
    [HttpPost]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]
    [SwaggerRequestExample(typeof(CreateRoleCommand), typeof(CreateRoleRequestExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.Create)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        BaseResponse response = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Bir rolün bilgilerini (adı ve normalize edilmiş adı) getirir.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `roleId` **route'tan** alınır; istek gövdesi/query string kabul edilmez.
    /// 2. Rol bulunamazsa **404** döner.
    /// 3. Yanıtta `id`, `name`, `normalizedName` ve `isSystemRole` alanları döner; liste ucundan farklı
    ///    olarak kullanıcı ve izin sayıları bu yanıtta yer almaz.
    /// Uç salt okunurdur ve `role:view` izniyle korunur.
    /// </remarks>
    /// <param name="roleId">Bilgileri getirilecek rolün benzersiz id'si.</param>
    /// <response code="200">Rol bilgisi döner.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:view` izni bulunmadığında.</response>
    /// <response code="404">Rol bulunamadığında ya da route'taki id geçerli bir GUID olmadığında.</response>
    [HttpGet("{roleId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<GetRoleInfoQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetRoleInfoResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.View)]
    public async Task<IActionResult> GetRoleInfo([FromRoute] Guid roleId)
    {
        var query = new GetRoleInfoQuery
        {
            RoleId = roleId
        };

        GeneralSuccessResponseWithData<GetRoleInfoQueryResult> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Bir rolü siler.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `roleId` **route'tan** alınır; istek gövdesi almaz.
    /// 2. Rol bulunamazsa **404** döner.
    /// 3. Sistem rolleri (`ADMIN` / `USER`) silinemez (**422**); bu roller yalnızca seed ile yönetilir.
    /// 4. Role bağlı kullanıcı varsa rol silinemez (**409**); önce kullanıcıların rolü değiştirilmelidir.
    /// 5. Silme tek `DELETE` ifadesiyle yapılır ve rolün izin bağlantıları (`role_permissions`) veritabanı
    ///    cascade kuralıyla birlikte silinir; bu sırada rol değişmiş ya da silinmişse (hiçbir satır
    ///    etkilenmez) **409** döner.
    /// 6. Başarılı silmeden sonra rol adına bağlı izin cache'i (`role:{rol}:permissions`) temizlenir.
    ///    Kullanıcı-rol cache'leri için ekstra iş yoktur: rolün kullanıcısı varsa silme zaten reddedilir.
    /// </remarks>
    /// <param name="roleId">Silinecek rolün benzersiz id'si.</param>
    /// <response code="200">Rol silindi; `CONTENT_DELETED_SUCCESS` kodu döner.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `role:delete` izni bulunmadığında.</response>
    /// <response code="404">Rol bulunamadığında ya da route'taki id geçerli bir GUID olmadığında.</response>
    /// <response code="409">Role bağlı kullanıcılar varsa ya da silme eşzamanlı bir değişiklik nedeniyle uygulanamazsa.</response>
    /// <response code="422">Sistem rolleri (`ADMIN` / `USER`) silinmek istendiğinde.</response>
    [HttpDelete("{roleId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentDeletedSuccessResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.RolePermissions.Delete)]
    public async Task<IActionResult> RemoveRole([FromRoute] Guid roleId)
    {
        var command = new RemoveRoleCommand
        {
            RoleId = roleId
        };

        BaseResponse response = await _mediator.Send(command);

        return Ok(response);
    }
}
