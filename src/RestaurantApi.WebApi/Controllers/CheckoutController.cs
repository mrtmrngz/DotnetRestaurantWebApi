using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Checkout.Commands.CreateOnlineCheckoutCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Giriş yapan kullanıcının sepetini siparişe dönüştürür ve Stripe üzerinden ödeme oturumu açar.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. `X-Idempotency-Key` ile daha önce sipariş oluşturulmuş mu kontrol edilir (mükerrer istek 409).
    /// 2. Kullanıcı, sepet ve sepette satışta olmayan ürünler doğrulanır.
    /// 3. Sepet kalemleri **indirimli birim fiyat** üzerinden siparişe aktarılır; kargo tutarı varsa Stripe'a ayrı "Kargo" kalemi olarak eklenir.
    /// 4. Stripe Checkout oturumu açılır (idempotency key sağlayıcıya da iletilir), dönen oturum id'si ve güvenli ödeme adresi siparişe kaydedilir.
    /// 5. Ödeme oturumu açıldıktan sonra sepetin içindeki ürünler temizlenir (sepet kaydı korunur).
    /// Tutarlar veritabanında TRY olarak tutulur, Stripe'a kuruş (en küçük birim) olarak gönderilir.
    /// </remarks>
    /// <param name="idempotencyKey">
    /// İstemci tarafından üretilen benzersiz istek anahtarı. Aynı ödeme denemesi tekrarlandığında
    /// aynı değer gönderilmeli, farklı siparişler için yeni bir anahtar üretilmelidir.
    /// </param>
    /// <response code="200">Ödeme oturumu oluşturuldu; güvenli ödeme sayfası adresi (`sessionUrl`) döner.</response>
    /// <response code="400">Doğrulama hataları veya `X-Idempotency-Key` header'ının gönderilmemesi.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="404">Kullanıcı, sepet ya da kullanıcının varsayılan adresi bulunamadığında.</response>
    /// <response code="409">Aynı idempotency key ile ödeme daha önce başlatıldığında veya sepette satışta olmayan ürün bulunduğunda.</response>
    /// <response code="422">Sepet boş olduğunda veya ödeme sağlayıcısı (Stripe) oturumu oluşturamadığında.</response>
    [HttpPost("users")]
    #region Swagger Docs
    [ProducesResponseType(typeof(CheckoutResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CheckoutSessionCreatedResponseExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize]
    public async Task<IActionResult> CreateCheckoutForUsers(
        [FromHeader(Name = "X-Idempotency-Key"), Required] string idempotencyKey
        )
    {
        var userId = _currentUserService.GetRequiredUserId();

        CreateOnlineCheckoutCommand command = new CreateOnlineCheckoutCommand()
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userId,
            IdempotencyKey = idempotencyKey
        };

        var response = await _mediator.Send(command);

        return Ok(response);
    }
}