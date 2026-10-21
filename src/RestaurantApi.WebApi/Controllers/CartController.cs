using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.CartExample;
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
}