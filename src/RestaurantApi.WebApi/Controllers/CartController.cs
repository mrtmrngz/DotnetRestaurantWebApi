using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;
using RestaurantApi.Application.Features.Cart.Commands.SetCartItemQuantityCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.CartExample;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public CartController(ICurrentUserService currentUserService, IMediator mediator)
    {
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    /// <summary>Kullanıcıların restoran dışına sipariş verebilir.</summary>
    /// <response code="200">Herşey yolunda ise.</response>
    /// <response code="400">
    /// Aşağıdaki durumlarda geçersiz istek (Bad Request) döner:
    /// - Validasyon hataları olduğunda.
    /// - Geçersiz işlem tipi geldiğinde.
    /// </response>
    /// <response code="401">Yetkisiz erişim (JWT Token eksik veya geçersiz).</response>
    /// <response code="404">
    /// Aşağıdaki durumlarda bu hata kodu döner:
    /// - Ürün gerçekten bulunamaz ise.
    /// - Ürün var fakat silindiyse (aynı hata mesajı döner).
    /// - Kullanıcı bulunamaz ise.
    /// </response>
    /// <response code="422">Sepet ürün miktarı 100 den daha fazla ise.</response>
    [HttpPost("users")]
    #region Swagger Docs
    [SwaggerRequestExample(typeof(AddToCartCommand), typeof(AddToCartRequestExample))]
    [ProducesResponseType(typeof(object), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestMultipleExamplesProvider))]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(UnprocessableEntityError), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize]
    public async Task<IActionResult> AddToCartForUsers([FromBody] AddToCartCommand command)
    {
        var currentUserId = _currentUserService.GetRequiredUserId();

        var newCommand = command with
        {
            RequestType = CartRequestTypes.CurrentUser, UserId = currentUserId, RestaurantTableId = null
        };

        var response = await _mediator.Send(newCommand);
        
        return Ok(response);
    }
    
    /// <summary>Restoran yetkilileri, restoran masaları için sepete ekleme işlemi yapabilir.</summary>
    /// <response code="200">Herşey yolunda ise.</response>
    /// <response code="400">
    /// Aşağıdaki durumlarda geçersiz istek (Bad Request) döner:
    /// - Validasyon hataları olduğunda.
    /// - Geçersiz işlem tipi geldiğinde.
    /// </response>
    /// <response code="401">Yetkisiz erişim (JWT Token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının bu rota için gerekli izne sahip değilse.</response>
    /// <response code="404">
    /// Aşağıdaki durumlarda bu hata kodu döner:
    /// - Ürün gerçekten bulunamaz ise.
    /// - Ürün var fakat silindiyse (aynı hata mesajı döner).
    /// - Restoranda böyle bir masa yok ise.
    /// </response>
    /// <response code="422">Sepet ürün miktarı 100 den daha fazla ise.</response>
    [HttpPost("tables/{tableId:guid}")]
    #region Swagger Docs
    [SwaggerRequestExample(typeof(AddToCartCommand), typeof(AddToCartRequestExample))]
    [ProducesResponseType(typeof(object), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestMultipleExamplesProvider))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(UnprocessableEntityError), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.CartPermissions.AddToCart)]
    public async Task<IActionResult> AddToCartForRestaurantTables([FromBody] AddToCartCommand command, [FromRoute] Guid tableId)
    {
        var newCommand = command with
        {
            RequestType = CartRequestTypes.PanelUser, RestaurantTableId = tableId, UserId = null
        };

        var response = await _mediator.Send(newCommand);
        
        return Ok(response);
    }

    /// <summary>Kullanıcı sepetteki ürün mikatatını güncellebilir.</summary>
    /// <response code="200">Sepetteki ürün miktarı güncellenir.</response>
    /// <response code="400">
    /// Şu durumda gerçekleşir:
    /// - Validasyon hataları meydana geldiyse.
    /// - Eğer geçersiz istek tipi geldiyse.
    /// </response>
    /// <response code="401">Yetkisiz erişim (JWT Token eksik veya geçersiz).</response>
    /// <response code="404">
    /// Şu durumda gerçekleşir:
    /// - Kullanıcı bulunamadığında.
    /// - Sepet bulunamadığında.
    /// - Sepetteki ürün bulunamadığında. 
    /// </response>
    [HttpPatch("users/items/{itemId:guid}/quantity")]
    #region Swagger Docs
    [SwaggerRequestExample(typeof(SetCartItemQuantityCommand), typeof(SetCartItemQuantityRequestExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    [ProducesResponseType(typeof(object), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestMultipleExamplesProvider))]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize]
    public async Task<IActionResult> SetCartItemQuantityForUsers([FromBody] SetCartItemQuantityCommand command,
        [FromRoute] Guid itemId)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var newCommand = command with
        {
            ItemId = itemId, UserId = userId, RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null
        };

        var response = await _mediator.Send(newCommand);

        return Ok(response);
    }
    
    /// <summary>Paneldeki yetki sahibi kullanıcılar sepetteki ürün mikatatını güncellebilir.</summary>
    /// <response code="200">Sepetteki ürün miktarı güncellenir.</response>
    /// <response code="400">
    /// Şu durumda gerçekleşir:
    /// - Validasyon hataları meydana geldiyse.
    /// - Eğer geçersiz istek tipi geldiyse.
    /// </response>
    /// <response code="401">Yetkisiz erişim (JWT Token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının bu işlem için gerekli izni yok.</response>
    /// <response code="404">
    /// Şu durumda gerçekleşir:
    /// - Masa bulunamadığında.
    /// - Sepet bulunamadığında.
    /// - Sepetteki ürün bulunamadığında. 
    /// </response>
    [HttpPatch("tables/{tableId:guid}/items/{itemId:guid}/quantity")]
    #region Swagger Docs
    [SwaggerRequestExample(typeof(SetCartItemQuantityCommand), typeof(SetCartItemQuantityRequestExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    [ProducesResponseType(typeof(object), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestMultipleExamplesProvider))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.CartPermissions.UpdateCart)]
    public async Task<IActionResult> SetCartItemQuantityForPanel([FromBody] SetCartItemQuantityCommand command,
        [FromRoute] Guid tableId, Guid itemId)
    {
        var newCommand = command with
        {
            ItemId = itemId, UserId = null, RequestType = CartRequestTypes.PanelUser, RestaurantTableId = tableId
        };

        var response = await _mediator.Send(newCommand);

        return Ok(response);
    }
}