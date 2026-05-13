using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Infrastructure.Settings;

namespace RestaurantApi.Infrastructure.Storage;

public class S3FileStorage: IFileStorage
{

    private readonly IAmazonS3 _s3;
    private readonly AwsSettings _settings;
    
    public S3FileStorage(IAmazonS3 s3, IOptions<AwsSettings> settings)
    {
        _s3 = s3;
        _settings = settings.Value;
    }


    public async Task<UploadFileResult> UploadAsync(IFormFile file)
    {
        var key = $"media/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType
        });

        return new UploadFileResult
        {
            PublicId = key,
            Url = $"{_settings.PublicBaseUrl}/{_settings.Bucket}/{key}",
            Size = file.Length,
            FileType = file.ContentType,
            FileExtension = Path.GetExtension(file.FileName)
        };
    }

    public async Task DeleteAsync(string key)
    {
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _settings.Bucket,
            Key = key
        });
    }
}