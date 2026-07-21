using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Commands;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AddressExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/addresses")]
public class AddressController: ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AddressController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    #region Swagger Documentation
    [SwaggerRequestExample(typeof(CreateAddressCommand), typeof(CreateAddressExample))]
    [ProducesResponseType(typeof(BaseResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(typeof(ValidationException), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]
    #endregion
    [Authorize(Policy = "SameUser")]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressCommand command)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updatedCommand = command with { UserId = userId };

        var response = await _mediator.Send(updatedCommand);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}