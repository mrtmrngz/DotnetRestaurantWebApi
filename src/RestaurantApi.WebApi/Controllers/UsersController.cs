using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;

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
}