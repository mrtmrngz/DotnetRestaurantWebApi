using System.Net;
using System.Text.Json;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using System.Text.Json.Serialization;

namespace RestaurantApi.WebApi.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    private static readonly JsonSerializerOptions CustomJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed | Path: {Path}", context.Request.Path);
            
            int statusCode = (int)HttpStatusCode.BadRequest;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new ValidationErrorResponse
            {
                StatusCode = statusCode,
                Errors = ex.Errors.Select(x => new ErrorList
                {
                    Field = x.PropertyName,
                    Message = x.ErrorMessage
                }).ToList()
            };

            await context.Response.WriteAsJsonAsync(response, CustomJsonOptions);
        }catch (Exception e)
        {
            await HandleExceptionAsync(context, e, _logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex, ILogger logger)
    {
        int statusCode = StatusCodes.Status500InternalServerError;
        Codes? code = null;
        string message = ex.Message;

        if (ex is AppException appException)
        {
            statusCode = appException.StatusCode;
            code = appException.Code;
            
            logger.LogWarning(
                ex,
                "Business exception | Code: {Code} | Path: {Path}",
                appException.Code,
                context.Request.Path
            );
        }
        else
        {
            logger.LogError(
                ex,
                "Unhandled exception | Path: {Path}",
                context.Request.Path
            );
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message,
            code,
            statusCode
        };

        return context.Response.WriteAsJsonAsync(response, CustomJsonOptions);
    }
}