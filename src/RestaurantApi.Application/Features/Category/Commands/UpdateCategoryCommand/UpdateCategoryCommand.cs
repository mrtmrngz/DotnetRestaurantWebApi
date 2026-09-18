using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Commands.UpdateCategoryCommand;

public record UpdateCategoryCommand(
    string? Title = null,
    IFormFile? Image = null
): IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore] 
    public Guid CategoryId { get; init; }
};