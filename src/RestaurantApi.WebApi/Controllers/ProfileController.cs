using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
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
}