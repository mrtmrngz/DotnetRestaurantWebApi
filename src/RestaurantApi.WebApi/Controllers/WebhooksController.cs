using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    // Stripe imzayı bu başlıkta gönderir.
    private const string StripeSignatureHeader = "Stripe-Signature";

    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Stripe checkout ödeme olaylarını işler.
    /// </summary>
    /// <remarks>
    /// İşlenen olaylar: `checkout.session.completed`, `checkout.session.async_payment_succeeded`,
    /// `checkout.session.async_payment_failed`, `checkout.session.expired`.
    ///
    /// Akış:
    /// 1. İstek gövdesi **ham (raw)** hâliyle okunur; imza doğrulaması gövdenin değiştirilmemiş hâli üzerinden yapılmalıdır.
    /// 2. `Stripe-Signature` başlığı ve ham gövde komuta aktarılır; imza doğrulaması ve olayın siparişe yansıtılması handler içindedir.
    /// 3. Bu uç **anonimdir** (Stripe token göndermez); güvenlik webhook imzasının doğrulanmasıyla sağlanır.
    ///
    /// Bu uç yalnızca Stripe'ın çağırması içindir; Stripe 2xx dışındaki yanıtlarda olayı tekrar gönderir,
    /// bu yüzden işleme adımları idempotent olmalıdır.
    /// </remarks>
    /// <param name="signature">Stripe'ın gönderdiği `Stripe-Signature` başlığı (zaman damgası ve imzayı taşır).</param>
    /// <param name="cancellationToken">İstemci bağlantısı kesildiğinde işlemi durdurmak için kullanılır.</param>
    /// <response code="200">Olay alındı ve işlendi; işlenmeyen olaylarda da 200 döner (Stripe tekrar denemesin).</response>
    /// <response code="400">İmza başlığı eksik/geçersiz, imza doğrulanamadı veya gövde çözümlenemedi.</response>
    /// <response code="404">Olayın ait olduğu sipariş bulunamadığında (kayıp ödeme olmaması için 2xx dönülmez).</response>
    /// <response code="409">Olayın oturumu siparişte kayıtlı ödeme oturumuyla eşleşmediğinde.</response>
    [HttpPost("checkout")]
    #region Swagger Docs
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(StripeWebhookProcessedResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    #endregion
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<IActionResult> CheckoutWebhookRoute(
        [FromHeader(Name = StripeSignatureHeader), Required] string signature,
        CancellationToken cancellationToken)
    {
        // İmza doğrulaması gövdenin ham hâli üzerinden yapıldığı için istek gövdesi elle okunur.
        using var reader = new StreamReader(Request.Body);

        string payload = await reader.ReadToEndAsync(cancellationToken);

        var command = new ProcessStripeWebhookCommand
        {
            Signature = signature,
            Payload = payload
        };

        BaseResponse response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }
}