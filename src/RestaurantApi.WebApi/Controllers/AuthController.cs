using MediatR;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Auth.Commands.Login;
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

    [HttpPost("login")]
    [SwaggerRequestExample(typeof(LoginCommand), typeof(LoginRequestExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(LoginExample))]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        LoginResponse result = await _mediator.Send(command);

        if (!string.IsNullOrEmpty(result.AccessToken) && !string.IsNullOrEmpty(result.RefreshToken))
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            };

            Response.Cookies.Append("_session", result.RefreshToken, cookieOptions);
            
            return Ok(new LoginControllerResponse
            {
                Message = result.Message,
                Code = result.Code,
                AccessToken = result.AccessToken,
            });
        }

        return Ok(new LoginControllerResponse
        {
            Message = result.Message,
            Code = result.Code,
        });
    }
}