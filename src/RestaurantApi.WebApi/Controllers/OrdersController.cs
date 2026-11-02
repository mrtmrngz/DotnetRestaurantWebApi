using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Orders.Commands.OrderCancelCommand;
using RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;
using RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;
using RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;
using RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;

using RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;
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

    /// <summary>
    /// Oturumdaki kullanıcının siparişlerini listeler (en yeni sipariş en üstte).
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. Kullanıcı id'si **token'dan** alınır; uç istek parametresi almaz, `userId` istemciden kabul edilmez.
    /// 2. Token geçerli olsa bile kullanıcı kaydı bulunamazsa **404** döner.
    /// 3. Siparişi olmayan kullanıcı için **200 + boş liste** döner; liste uçlarında "kayıt yok" bir hata değildir.
    /// Her satırda `orderId`, `subTotal`, `shippingCost`, `total`, `orderStatus` ve `orderDate` alanları bulunur.
    /// Masa siparişleri kullanıcıya bağlı olmadığı için bu listede yer almaz.
    /// </remarks>
    /// <response code="200">Kullanıcının siparişleri (boş liste olabilir) döner.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcı kendi kaynağına erişmediğinde (`SameUser` policy).</response>
    /// <response code="404">Kullanıcı bulunamadığında.</response>
    [HttpGet("users")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetUserOrdersResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> GetUserOrders()
    {
        var query = new GetUserOrdersQuery
        {
            UserId = _currentUserService.GetRequiredUserId()
        };

        GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Oturumdaki kullanıcının tek bir siparişinin detayını döndürür.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `orderId` **route'tan**, kullanıcı id'si **token'dan** alınır; ikisi de istek gövdesinden kabul edilmez.
    /// 2. Token geçerli olsa bile kullanıcı kaydı bulunamazsa **404** döner.
    /// 3. Sipariş bulunamazsa **404**; sipariş isteği yapan kullanıcıya ait değilse bilgi sızdırmamak için yine
    ///    **404** döner (gerçek sebep yalnızca sunucu loglarında görünür).
    /// 4. Detayda `orderStatus`, `paymentStatus`, `subTotal`, `shippingCost`, `total`, `addressSnapshot` ve sipariş
    ///    kalemleri (`orderItems`: `productName`, `unitPrice`, `quantity`) döner.
    /// Kullanıcı siparişlerinde adres zorunlu olduğu için `addressSnapshot` dolu gelir; değer, sipariş anındaki
    /// adresin JSON anlık görüntüsüdür. Masa siparişleri kullanıcıya bağlı olmadığı için bu uçtan açılamaz.
    /// </remarks>
    /// <param name="orderId">Detayı getirilecek siparişin benzersiz id'si.</param>
    /// <response code="200">Sipariş detayı `FETH_DATA_SUCCESS` kodu ile döner.</response>
    /// <response code="400">Doğrulama hataları (sipariş ID'si geçersiz/boş ya da kullanıcı ID'si boş olduğunda).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcı kendi kaynağına erişmediğinde (`SameUser` policy).</response>
    /// <response code="404">Kullanıcı bulunamadığında ya da sipariş bulunamadığında/başka bir kullanıcıya ait olduğunda.</response>
    [HttpGet("users/{orderId}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetUserOrderDetailResponseExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> GetUserOrderDetail([FromRoute] Guid orderId)
    {
        var query = new GetUserOrderDetailQuery
        {
            OrderId = orderId,
            UserId = _currentUserService.GetRequiredUserId()
        };

        GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult> response = await _mediator.Send(query);

        return Ok(response);
    }


    /// <summary>
    /// Bir masanın siparişlerini listeler (en yeni sipariş en üstte); panel akışıdır.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `tableId` route'tan alınır; masa kaydı bulunamazsa **404** döner.
    ///    Aktif/pasif ayrımı yapılmaz: pasif masanın siparişleri de listelenir.
    /// 2. Siparişi olmayan masa için **200 + boş liste** döner (liste uçlarında "kayıt yok" bir hata değildir).
    /// Her satırda `orderId`, `subTotal`, `shippingCost`, `total`, `orderStatus` ve `orderDate` alanları bulunur.
    /// Masa siparişleri kullanıcıya bağlı olmadığı için kullanıcı sipariş listesinde yer almaz; bu uç yalnızca masaya bağlı siparişleri döndürür.
    /// </remarks>
    /// <param name="tableId">Siparişleri listelenecek masanın benzersiz id'si.</param>
    /// <response code="200">Masanın siparişleri (boş liste olabilir) döner.</response>
    /// <response code="400">Masa ID geçersiz veya boş olduğunda.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `orders:read` izni bulunmadığında.</response>
    /// <response code="404">Masa bulunamadığında.</response>
    [HttpGet("tables/{tableId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetTableOrdersResponseExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.OrderPermissions.Read)]
    public async Task<IActionResult> GetTableOrders([FromRoute] Guid tableId)
    {
        var query = new GetTableOrdersQuery
        {
            TableId = tableId
        };

        GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }


    /// <summary>
    /// Panel: tüm siparişleri (kullanıcı + masa) filtreleyerek listeler; en yeni sipariş en üstte.
    /// </summary>
    /// <remarks>
    /// Filtreler opsiyoneldir; hiçbiri gönderilmezse tüm siparişler döner:
    /// - `type`: `table` (yalnız masa siparişleri) veya `users` (yalnız kullanıcı siparişleri).
    /// - `order-status`: `OrderStatus` enum adı (ör. `Delivered`).
    /// - `payment-status`: `PaymentStatus` enum adı (ör. `Paid`).
    ///
    /// Her satırda `orderId`, `subTotal`, `shippingCost`, `total`, `orderStatus`, `orderDate` alanlarına ek olarak
    /// `userId`, `tableId` (sipariş sahibine göre biri boş olur) ve `type` (`user` / `table`) döner.
    ///
    /// Desteklenmeyen bir `type` değeri ya da enum'larla eşleşmeyen bir `order-status`/`payment-status` değeri
    /// gönderilirse (ör. `type=owner`, `order-status=Foo`) istek hata dönmez; **200 + boş liste** döner.
    /// </remarks>
    /// <param name="type">Sipariş sahibi tipi filtresi (`table` veya `users`); boşsa filtre uygulanmaz.</param>
    /// <param name="orderStatus">Sipariş durumu filtresi (`OrderStatus` enum adı); boşsa filtre uygulanmaz.</param>
    /// <param name="paymentStatus">Ödeme durumu filtresi (`PaymentStatus` enum adı); boşsa filtre uygulanmaz.</param>
    /// <response code="200">Filtreye uyan siparişler (boş liste olabilir) döner.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `orders:read` izni bulunmadığında.</response>
    [HttpGet("panel")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetAllOrdersResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    #endregion
    [Authorize(Policy = Permissions.OrderPermissions.Read)]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? type,
        [FromQuery(Name = "order-status")] string? orderStatus,
        [FromQuery(Name = "payment-status")] string? paymentStatus)
    {
        var query = new GetAllOrdersQuery
        {
            Type = type,
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus
        };

        GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Panel: tek bir siparişin detayını (masa ya da kullanıcı siparişi) döndürür.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `orderId` **route'tan** alınır; sipariş bulunamazsa **404** döner.
    /// 2. Detayda `orderStatus`, `paymentStatus`, `subTotal`, `shippingCost`, `total`, `orderDate`,
    ///    `addressSnapshot` ve sipariş kalemleri (`orderItems`: `productName`, `unitPrice`, `quantity`) döner.
    /// 3. Sipariş sahibine göre satır alanları değişir: masa siparişlerinde `tableNumber` (masa numarası),
    ///    kullanıcı siparişlerinde müşteri tam adı (`customerFullName`) doludur; kullanıcı silinmişse ad boş döner.
    /// 4. Adres anlık görüntüsü (`addressSnapshot`) kullanıcı siparişlerinde zorunlu olduğu için doludur,
    ///    masa siparişlerinde boş olabilir.
    /// Bu uç yalnızca `orders:read` izni olan panel kullanıcıları içindir; sahiplik kontrolü yapılmaz.
    /// </remarks>
    /// <param name="orderId">Detayı getirilecek siparişin benzersiz id'si.</param>
    /// <response code="200">Sipariş detayı `FETH_DATA_SUCCESS` kodu ile döner.</response>
    /// <response code="400">Doğrulama hataları (sipariş ID'si geçersiz/boş olduğunda).</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `orders:read` izni bulunmadığında.</response>
    /// <response code="404">Sipariş bulunamadığında.</response>
    [HttpGet("panel/{orderId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetPanelOrderDetailResponseExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.OrderPermissions.Read)]
    public async Task<IActionResult> GetPanelOrderDetail([FromRoute] Guid orderId)
    {
        var query = new GetPanelOrderDetailQuery
        {
            OrderId = orderId
        };

        GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult> response = await _mediator.Send(query);

        return Ok(response);
    }

}
