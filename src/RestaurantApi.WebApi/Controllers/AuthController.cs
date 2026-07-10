using MediatR;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Features.Auth.Commands.Logout;
using RestaurantApi.Application.Features.Auth.Commands.MailVerify;
using RestaurantApi.Application.Features.Auth.Commands.RefreshToken;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Features.Auth.Commands.TwoFactorLogin;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
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

    [HttpPost("mail-verify")]
    #region Swagger Documentation

    [SwaggerRequestExample(typeof(MailVerifyCommand), typeof(MailVerifyExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(MailVerifiedResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestErrorExample))]

    #endregion
    public async Task<IActionResult> MailVerify([FromBody] MailVerifyCommand command)
    {
        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [HttpPost("two-factor/login")]
    #region SwaggerDocumentation
    [SwaggerRequestExample(typeof(TwoFactorLoginCommand), typeof(TwoFactorLoginExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(LoginExample))]
    [ProducesResponseType(typeof(object), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestMultipleExamplesProvider))]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedExceptionExample))]
    #endregion
    public async Task<IActionResult> TwoFactorLogin([FromBody] TwoFactorLoginCommand command)
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

    [HttpPost("refresh")]
    #region Swagger Documentation
    [ProducesResponseType(typeof(LoginControllerResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(RefreshTokenExample))]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedExceptionExample))]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    public async Task<IActionResult> RefreshToken()
    {
        var token = Request.Cookies["_session"];
        var result = await _mediator.Send(new RefreshTokenCommand(Token: token));

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/"
        };

        Response.Cookies.Append("_session", result.RefreshToken!, cookieOptions);

        return Ok(new LoginControllerResponse
        {
            Message = result.Message,
            Code = result.Code,
            AccessToken = result.AccessToken,
        });
    }

    [HttpPost("forgot-password/verify")]
    #region Swagger Examples
    [SwaggerRequestExample(typeof(ForgotPasswordVerifyCommand), typeof(ForgotPasswordVerifyExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(MailSentResponseExample))]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    #endregion
    public async Task<IActionResult> ForgotPasswordVerify([FromBody] ForgotPasswordVerifyCommand command)
    {
        var response = await _mediator.Send(command);
        
        return Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(LogoutExample))]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["_session"];

        if (!string.IsNullOrEmpty(token))
        {
            var command = new LogoutCommand(token);
            await _mediator.Send(command);
            
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
                Path = "/"
            };

            Response.Cookies.Delete("_session", cookieOptions);
        }
        
        var baseResponse = new BaseResponse
        {
            Code = Codes.LOGOUT_SUCCESS,
            Message = "Başarıyla çıkış yapıldı."
        };

        return Ok(baseResponse);
    }
}