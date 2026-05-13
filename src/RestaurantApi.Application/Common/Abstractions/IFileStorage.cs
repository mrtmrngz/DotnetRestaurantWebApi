using Microsoft.AspNetCore.Http;
using RestaurantApi.Application.Features.Files.Dtos;

namespace RestaurantApi.Application.Common.Abstractions;

public interface IFileStorage
{
    Task<UploadFileResult> UploadAsync(IFormFile file);
    Task DeleteAsync(string key);
}