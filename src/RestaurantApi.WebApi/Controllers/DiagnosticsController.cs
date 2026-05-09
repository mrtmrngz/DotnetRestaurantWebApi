using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Infrastructure.Cache;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("cache")]
    public IActionResult CacheStats()
    {
        return Ok(CacheMetrics.GetStats());
    }
}