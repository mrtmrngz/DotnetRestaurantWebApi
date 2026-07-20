using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.ProfileHandlers.Commands.ChangePasswordCommand;
using RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public ProfileController(ICurrentUserService currentUserService, IMediator mediator)
    {
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> ProfileInfo()
    {
        Guid userId = _currentUserService.GetRequiredUserId();
        var query = new ProfileInfoQuery(userId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var userId = _currentUserService.GetRequiredUserId();

        var updatedCommand = command with { UserId = userId };

        var response = await _mediator.Send(updatedCommand);

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/"
        };

        Response.Cookies.Delete("_session", cookieOptions);

        return Ok(response);
    }
}