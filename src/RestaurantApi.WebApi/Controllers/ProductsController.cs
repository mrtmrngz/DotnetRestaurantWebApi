using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Products.Commands.CreateProductCommand;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]
    [ProducesResponseType(typeof(BaseResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.ProductPermissions.Create)]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductCommand command)
    {
        var response = await _mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}