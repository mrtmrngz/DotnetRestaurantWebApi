using Microsoft.AspNetCore.Http;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Files.Dtos;

namespace RestaurantApi.IntegrationTests.Setup;

public class FakeFileStorage : IFileStorage
{
    public Task<UploadFileResult> UploadAsync(IFormFile file)
    {
        return Task.FromResult(new UploadFileResult
        {
            PublicId = $"media/test_{Guid.NewGuid():N}.png",
            Url = $"https://fake-s3.local/bucket/test.png",
            Size = file.Length,
            FileType = file.ContentType,
            FileExtension = Path.GetExtension(file.FileName)
        });
    }

    public Task DeleteAsync(string key) => Task.CompletedTask;

    public Task<List<UploadFileResult>> UploadMultipleAsync(List<IFormFile> files)
    {
        var results = files.Select(f => UploadAsync(f).Result).ToList();
        return Task.FromResult(results);
    }
}