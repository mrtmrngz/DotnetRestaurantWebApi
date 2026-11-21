using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Infrastructure.Cache;
using RestaurantApi.WebApi.Configurations.RateLimiting.Policies;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Panel.GeneralRead)]
public class DiagnosticsController : ControllerBase
{
    /// <summary>
    /// Redis cache hit/miss sayaçlarını döner.
    /// </summary>
    /// <remarks>
    /// Akış:
    /// 1. İstek parametre almaz; uç yalnızca süreç içi sayaçları okur (Redis'e sorgu atmaz).
    /// 2. Yanıtta `hit`, `miss` ve `hitRatio` (yüzde metni, ör. `"85.00%"`) döner.
    /// Uç, cache verimliliğini izlemek için kullanılır ve `diagnostics:view` izniyle korunur;
    /// bu nedenle anonim erişime kapalıdır.
    /// </remarks>
    /// <response code="200">Cache sayaçları döner.</response>
    /// <response code="401">Yetkisiz erişim (JWT token eksik veya geçersiz).</response>
    /// <response code="403">Kullanıcının `diagnostics:view` izni bulunmadığında.</response>
    [HttpGet("cache")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(CacheMetricsStats), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CacheMetricsResponseExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    #endregion
    [Authorize(Policy = Permissions.DiagnosticsPermissions.View)]
    public IActionResult CacheStats()
    {
        return Ok(CacheMetrics.GetStats());
    }
}
