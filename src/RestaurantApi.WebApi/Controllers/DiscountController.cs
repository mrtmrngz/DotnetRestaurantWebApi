using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Discount.Commands.AddDiscountCommand;
using RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Enums;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/discounts")]
public class DiscountController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AdminDiscountListResponseExample))]
    #endregion
    [Authorize(Policy = Permissions.DiscountPermissions.View)]
    public async Task<IActionResult> GetDiscounts([FromQuery] DiscountStatus? status)
    {
        GetDiscountListQuery query = new GetDiscountListQuery(status);

        GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>> response =
            await _mediator.Send(query);

        return Ok(response);
    }

    [HttpPost]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]
    [ProducesResponseType(typeof(BaseResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ConflictException), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    [ProducesResponseType(typeof(UnprocessableEntityError), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.DiscountPermissions.Create)]
    public async Task<IActionResult> AddDiscount([FromBody] AddDiscountCommand command)
    {
        BaseResponse response = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
