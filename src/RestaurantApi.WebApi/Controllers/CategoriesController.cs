using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;
using RestaurantApi.Application.Features.Category.Queries.AdminCategoryListQuery;
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
}