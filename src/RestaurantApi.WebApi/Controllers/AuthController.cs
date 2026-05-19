using MediatR;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AuthExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{

    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [SwaggerRequestExample(typeof(RegisterCommand), typeof(RegisterRequestExample))]
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var response = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}