using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;
using RestaurantApi.Application.Features.Category.Commands.DeleteCategoryCommand;
using RestaurantApi.Application.Features.Category.Commands.UpdateCategoryCommand;
using RestaurantApi.Application.Features.Category.Queries.AdminCategoryListQuery;
using RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;
using RestaurantApi.Application.Features.Category.Queries.GetCategoriesQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public CategoriesController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost]

    #region Swagger Documentation

    [ProducesResponseType(typeof(ValidationException), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(BaseResponse), 201)]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ContentCreatedExample))]

    #endregion

    [Authorize(Policy = Permissions.CategoryPermissions.Create)]
    public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryCommand command)
    {
        var response = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("public")]

    #region Swagger Documentation

    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(PublicCategoryListExample))]

    #endregion

    public async Task<IActionResult> GetCategories()
    {
        var response = await _mediator.Send(new GetCategoryQuery());

        return Ok(response);
    }

    [HttpGet("admin")]

    #region Swagger Documentation

    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AdminCategoryListResponseExample))]

    #endregion

    [Authorize(Policy = Permissions.CategoryPermissions.View)]
    public async Task<IActionResult> GetAdminCategories()
    {
        var response = await _mediator.Send(new AdminCategoryListQuery());

        return Ok(response);
    }

    [HttpGet("{categoryId:guid}")]

    #region MyRegion

    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(GeneralSuccessResponseWithData<CategoryDetailQueryResult>), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CategoryDetailResponseExample))]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]

    #endregion

    [Authorize(Policy = Permissions.CategoryPermissions.View)]
    public async Task<IActionResult> GetCategoryDetail([FromRoute] Guid categoryId)
    {
        var response = await _mediator.Send(new CategoryDetailQuery(CategoryId: categoryId));

        return Ok(response);
    }

    [HttpPatch("{categoryId:guid}")]
    #region Swagger Documantation
    [ProducesResponseType(typeof(BadRequestException), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [ProducesResponseType( 401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(NotFoundException), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentUpdatedResponseExample))]
    #endregion
    [Authorize(Policy = Permissions.CategoryPermissions.Update)]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid categoryId,
        [FromForm] UpdateCategoryCommand command)
    {
        command ??= new UpdateCategoryCommand();
    
        var commandToSend = command with { CategoryId = categoryId };
        
        var response = await _mediator.Send(commandToSend);

        return Ok(response);
    }

    [HttpDelete("{categoryId:guid}")]
    #region 
    [ProducesResponseType( 401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(BaseResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 422)]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnproccesableEntityErrorExample))]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ContentDeletedSuccessResponseExample))]
    #endregion
    [Authorize(Policy = Permissions.CategoryPermissions.Delete)]
    public async Task<IActionResult> DeleteCategory([FromRoute] Guid categoryId)
    {
        var command = new DeleteCategoryCommand(categoryId);

        var response = await _mediator.Send(command);

        return Ok(response);
    }
}