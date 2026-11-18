using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;
using RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
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
}
