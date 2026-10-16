using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.RestaurantTable.Commands.CreateRestaurantTableCommand;
using RestaurantApi.Application.Features.RestaurantTable.Commands.DeleteRestaurantTableCommand;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TablesController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AdminRestaurantTableListResponseExample))]
    [ProducesResponseType(typeof(BaseResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.TablePermissions.View)]
    public async Task<IActionResult> GetTables([FromQuery] GetRestaurantTableListQuery query)
    {
        GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>> response =
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
    [ProducesResponseType(typeof(ConflictException), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.TablePermissions.Create)]
    public async Task<IActionResult> CreateTable([FromBody] CreateRestaurantTableCommand command)
    {
        BaseResponse response = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpDelete("{tableId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentDeletedSuccessResponseExample))]
    #endregion
    [Authorize(Policy = Permissions.TablePermissions.Delete)]
    public async Task<IActionResult> DeleteTable([FromRoute] Guid tableId)
    {
        DeleteRestaurantTableCommand command = new DeleteRestaurantTableCommand { TableId = tableId };

        BaseResponse response = await _mediator.Send(command);

        return Ok(response);
    }
}