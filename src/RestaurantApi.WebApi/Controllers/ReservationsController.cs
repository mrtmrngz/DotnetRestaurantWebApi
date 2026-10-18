using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Reservation.Commands.CreateCurrentUserReservationCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.RequestExamples.ReservationExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ReservationsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost("add/current-user")]
    #region Swagger Documentation
    [SwaggerRequestExample(typeof(CreateCurrentUserReservationCommand),
        typeof(CreateCurrentUserReservationRequestExample))]
    [ProducesResponseType(typeof(ValidationException), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ConflictException), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]
    #endregion
    [Authorize(Policy = Permissions.ReservationPermissions.Create)]
    public async Task<IActionResult> CreateCurrentUserReservation([FromBody] CreateCurrentUserReservationCommand command)
    {
        var userId = _currentUserService.GetRequiredUserId();

        CreateCurrentUserReservationCommand commandToSend = command with { UserId = userId };

        BaseResponse response = await _mediator.Send(commandToSend);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
