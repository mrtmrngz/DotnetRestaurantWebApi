using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Orders.Commands.OrderCancelCommand;
using RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.OrderExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public OrdersController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Kullanıcı siparişinin durumunu günceller.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `orderId` route'tan alınır ve istek gövdesinden **kabul edilmez**; `userId` ile `newStatus` gövdeden okunur.
    /// 2. Sipariş yoksa **404**, sipariş isteği yapan kullanıcıya ait değilse **403** döner
    ///    (masa siparişlerinde `UserId` boş olduğu için masa siparişleri bu uçtan güncellenemez).
    /// 3. İstenen durum, siparişin mevcut durumuyla aynıysa **409** döner.
    /// 4. Ödemesi tamamlanmayan siparişin durumu güncellenemez (**422**).
    /// 5. Güncelleme tek `UPDATE` ifadesiyle yapılır; bu sırada sipariş değişmişse (ör. ödeme iptal edildi) **409** döner.
    /// Bu uç yalnızca kullanıcı siparişinin durumunu günceller; masa siparişlerinin mutfak akışı kapsam dışıdır.
    /// </remarks>
    /// <param name="orderId">Durumu güncellenecek siparişin benzersiz id'si.</param>
    /// <param name="command">Gövde: `userId` (zorunlu) ve `newStatus` (zorunlu, `OrderStatus`).</param>
    /// <response code="200">Sipariş durumu güncellendi; `CONTENT_UPDATED_SUCCESS` kodu döner.</response>
    /// <response code="400">Doğrulama hataları (eksik veya geçersiz alanlar).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `orders:update` izni yoksa ya da sipariş başka bir kullanıcıya aitse.</response>
    /// <response code="404">Sipariş bulunamadığında.</response>
    /// <response code="409">İstenen durum mevcut durumla aynıysa veya güncelleme eşzamanlı bir değişiklik nedeniyle uygulanamazsa.</response>
    /// <response code="422">Siparişin ödemesi tamamlanmadıysa.</response>
    [HttpPatch("users/status/{orderId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    [SwaggerRequestExample(typeof(OrderStatusUpdateCommand), typeof(OrderStatusUpdateRequestExample))]
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
    [Authorize(Policy = Permissions.OrderPermissions.Update)]
    public async Task<IActionResult> OrderStatusUpdate([FromRoute] Guid orderId,
        [FromBody] OrderStatusUpdateCommand command)
    {
        var commandToSend = command with { OrderId = orderId };

        BaseResponse response = await _mediator.Send(commandToSend);

        return Ok(response);
    }

    /// <summary>
    /// Oturumdaki kullanıcının kendi siparişini iptal eder.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `orderId` route'tan alınır, kullanıcı id'si **token'dan** okunur; istek gövdesi yoktur.
    /// 2. Sipariş yoksa **404**; sipariş başka bir kullanıcıya aitse bilgi sızdırmamak için yine **404** döner
    ///    (gerçek sebep yalnızca sunucu loglarında görünür).
    /// 3. Ödemesi tamamlanmayan sipariş iptal edilemez (**422**).
    /// 4. İptal edilmiş sipariş **409**, teslim edilmiş sipariş **422** ile reddedilir.
    /// 5. İptal, tek `UPDATE` ifadesiyle yapılır: sipariş `Canceled`, ödeme durumu `Refunded` olarak yazılır
    ///    (Stripe üzerinden gerçek iade akışı henüz yoktur); bu sırada sipariş değişmişse **409** döner.
    /// Masa siparişleri kullanıcıya bağlı olmadığı için bu uçtan iptal edilemez.
    /// </remarks>
    /// <param name="orderId">İptal edilecek siparişin benzersiz id'si.</param>
    /// <response code="200">Sipariş iptal edildi ve ödeme durumu iade olarak işaretlendi; `CONTENT_UPDATED_SUCCESS` kodu döner.</response>
    /// <response code="400">Doğrulama hataları.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcı kendi kaynağına erişmediğinde (`SameUser` policy).</response>
    /// <response code="404">Sipariş bulunamadığında ya da sipariş isteği yapan kullanıcıya ait olmadığında.</response>
    /// <response code="409">Sipariş zaten iptal edilmişse veya güncelleme eşzamanlı bir değişiklik nedeniyle uygulanamazsa.</response>
    /// <response code="422">Siparişin ödemesi tamamlanmadıysa ya da sipariş teslim edilmişse.</response>
    [HttpPatch("users/cancel/{orderId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
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
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> OrderCancel([FromRoute] Guid orderId)
    {
        var command = new OrderCancelCommand
        {
            OrderId = orderId,
            UserId = _currentUserService.GetRequiredUserId()
        };

        BaseResponse response = await _mediator.Send(command);

        return Ok(response);
    }
}
