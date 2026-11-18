using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;
using RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;
using RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;
using RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public UsersController(ICurrentUserService currentUserService, IMediator mediator)
    {
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    [HttpGet("me")]
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> GetUserInfoForLogin()
    {
        string? userId = _currentUserService.UserId;

        var query = new GetMeQuery(userId!);

        var response = await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Panel: sistemdeki tüm kullanıcıları rolleriniyle birlikte listeler.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. İstek parametre almaz; kullanıcılar ad ve soyada göre alfabetik döner.
    /// 2. Her satırda `id`, `fullName`, `email`, `phoneNumber`, `emailConfirmed`, `twoFactorStatus` ve `roles` döner.
    /// 3. `fullName` ad ve soyadın birleşimidir; `twoFactorStatus` kullanıcının iki adımlı doğrulama ayarıdır.
    /// 4. `roles`, kullanıcının rol **adları** dizisidir; rolü olmayan kullanıcı için boş dizi döner.
    /// 5. Liste tek projeksiyon sorgusuyla, roller ise kullanıcı-rol join tablosundan tek gruplama sorgusuyla
    ///    alınır (kullanıcı başına ayrı sorgu atılmaz); silinmiş (soft delete) kullanıcılar da listede döner.
    /// Hiç kullanıcı yoksa **200 + boş liste** döner.
    /// </remarks>
    /// <response code="200">Kullanıcılar alfabetik olarak döner (boş liste olabilir).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `user:view` izni bulunmadığında.</response>
    [HttpGet]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetUserListResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    #endregion
    [Authorize(Policy = Permissions.UserPermissions.View)]
    public async Task<IActionResult> GetUserList()
    {
        var query = new GetUserListQuery();

        GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Bir kullanıcının bilgilerini ve rollerini getirir.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `userId` **route'tan** alınır; istek gövdesi/query string kabul edilmez.
    /// 2. Kullanıcı bulunamazsa **404** döner.
    /// 3. Yanıtta `id`, `name`, `surname`, `email`, `emailConfirmed`, `phoneNumber`, `phoneNumberConfirmed`,
    ///    `twoFactorEnabled` ve `roles` döner.
    /// 4. `roles`, kullanıcının rol **adları** dizisidir; rolü olmayan kullanıcı için boş dizi döner.
    /// Kullanıcı tek sorguda okunur, roller ise kullanıcı-rol join tablosundan tek gruplama sorgusuyla alınır
    /// ve sonuç birleştirilir (kullanıcı başına ek sorgu atılmaz).
    /// Uç salt okunurdur ve `user:view` izniyle korunur.
    /// </remarks>
    /// <param name="userId">Bilgileri getirilecek kullanıcının benzersiz id'si.</param>
    /// <response code="200">Kullanıcı bilgisi döner.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `user:view` izni bulunmadığında.</response>
    /// <response code="404">Kullanıcı bulunamadığında ya da route'taki id geçerli bir GUID olmadığında.</response>
    [HttpGet("{userId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<GetUserInfoQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetUserInfoResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.UserPermissions.View)]
    public async Task<IActionResult> GetUserInfo([FromRoute] Guid userId)
    {
        var query = new GetUserInfoQuery
        {
            UserId = userId
        };

        GeneralSuccessResponseWithData<GetUserInfoQueryResult> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Bir kullanıcının rol durumunu listeler: tüm roller döner ve kullanıcının sahip olduğu roller işaretlenir.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `userId` **route'tan** alınır; istek gövdesi/query string kabul edilmez.
    /// 2. Kullanıcı bulunamazsa **404** döner.
    /// 3. İstek **tüm rolleri** döner; her satırda `id` (rol id), `name` (rol adı) ve `hasRole`
    ///    (kullanıcı bu role sahipse **true**, değilse **false**) bulunur.
    /// 4. Roller `name` alanına göre alfabetik sıralanır; `hasRole`, kullanıcı-rol join tablosundan tek
    ///    gruplama sorgusuyla elde edilen rol adlarıyla karşılaştırılarak işaretlenir.
    /// Uç, kullanıcıya rol atama ekranını besler ve `user:update` izniyle korunur.
    /// </remarks>
    /// <param name="userId">Rol durumu listelenecek kullanıcının benzersiz id'si.</param>
    /// <response code="200">Tüm roller ve kullanıcının sahiplik durumu döner (boş liste olabilir).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `user:update` izni bulunmadığında.</response>
    /// <response code="404">Kullanıcı bulunamadığında ya da route'taki id geçerli bir GUID olmadığında.</response>
    [HttpGet("{userId:guid}/roles")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetUserRolesResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.UserPermissions.Update)]
    public async Task<IActionResult> GetUserRoles([FromRoute] Guid userId)
    {
        var query = new GetUserRolesQuery
        {
            UserId = userId
        };

        GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }
}
