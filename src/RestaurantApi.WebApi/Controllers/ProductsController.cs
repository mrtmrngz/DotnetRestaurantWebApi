using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Products.Commands.CreateProductCommand;
using RestaurantApi.Application.Features.Products.Commands.UpdateProductCommand;
using RestaurantApi.Application.Features.Products.Queries.AdminProductDetailQuery;
using RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;
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

    [HttpGet("public")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(PublicProductListExample))]
    #endregion
    public async Task<IActionResult> ProductsListForPublic()
    {
        PublicProductsListQuery query = new PublicProductsListQuery();
        GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>> response = await _mediator.Send(query);
        return Ok(response);
    }
    
    [HttpGet("admin")]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AdminProductListResponseExample))]
    #endregion
    [Authorize(Policy = Permissions.ProductPermissions.View)]
    public async Task<IActionResult> ProductsListForAdmin()
    {
        AdminProductListQuery query = new AdminProductListQuery();
        GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>> response = await _mediator.Send(query);
        return Ok(response);
    }
    
    [HttpGet("public/{slug:required}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<PublicProductDetailQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(PublicProductDetailExample))]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    public async Task<IActionResult> ProductDetailForPublic([FromRoute] string slug)
    {
        PublicProductDetailQuery command = new PublicProductDetailQuery() with { Slug = slug };
        GeneralSuccessResponseWithData<PublicProductDetailQueryResult> response = await _mediator.Send(command);

        return Ok(response);
    }
    
    [HttpGet("admin/{productId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<AdminProductDetailQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AdminProductDetailResponseExample))]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    #endregion
    [Authorize(Policy = Permissions.ProductPermissions.View)]
    public async Task<IActionResult> ProductDetailForAdmin([FromRoute] Guid productId)
    {
        AdminProductDetailQuery command = new AdminProductDetailQuery { ProductId = productId};
        GeneralSuccessResponseWithData<AdminProductDetailQueryResult> response = await _mediator.Send(command);

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
    #endregion
    [Authorize(Policy = Permissions.ProductPermissions.Create)]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductCommand command)
    {
        var response = await _mediator.Send(command);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{productId:guid}")]
    #region SwaggerDocumentation
    [ProducesResponseType(typeof(BadRequestException), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    #endregion
    [Authorize(Policy = Permissions.ProductPermissions.Update)]
    public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductCommand command, Guid productId)
    {
        command ??= new UpdateProductCommand();

        var commandToSend = command with { ProductId = productId };

        var response = await _mediator.Send(commandToSend);

        return Ok(response);
    }
}