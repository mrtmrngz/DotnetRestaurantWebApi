using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Infrastructure.Settings;

namespace RestaurantApi.Infrastructure.Storage;

public class S3FileStorage: IFileStorage
{

    private readonly IAmazonS3 _s3;
    private readonly AwsSettings _settings;
    private readonly ILogger<S3FileStorage> _logger;
    
    public S3FileStorage(IAmazonS3 s3, IOptions<AwsSettings> settings, ILogger<S3FileStorage> logger)
    {
        _s3 = s3;
        _logger = logger;
        _settings = settings.Value;
    }
    
    public async Task<UploadFileResult> UploadAsync(IFormFile file)
    {
        var fileName = file.FileName;
        try
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
                FileExtension = Path.GetExtension(fileName)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("🔥 [S3Storage] '{fileName}' yüklenirken hata oluştu! Mesaj: {message}", fileName, ex.Message);
            throw;
        }
    }

    public async Task DeleteAsync(string key)
    {
        try
        {
            await _s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _settings.Bucket,
                Key = key
            });
        }
        catch (Exception ex)
        {
            _logger.LogCritical("⚠️ [CRITICAL] Rollback sırasında dosya silinemedi! Manuel müdahale gerekebilir: {key}. Hata: {err}", key, ex.Message);
            throw;
        }
    }

    public async Task<List<UploadFileResult>> UploadMultipleAsync(List<IFormFile> files)
    {
        var uploadedResults = new List<UploadFileResult>();
        
        try
        {
            foreach (var file in files)
            {
                var result = await UploadAsync(file);
                uploadedResults.Add(result);
            }

            return uploadedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError("🔥 [S3Storage] Toplu yükleme sırasında hata oluştu! Hata: {message}", ex.Message);
            if (uploadedResults.Any())
            {
                _logger.LogWarning("🧹 [Rollback] Kısmi yüklenen {count} adet dosya temizleniyor...", uploadedResults.Count);
            
                foreach (var result in uploadedResults)
                {
                    try 
                    {
                        await DeleteAsync(result.PublicId);
                        _logger.LogInformation("🗑️ [Deleted] Çöp dosya silindi: {key}", result.PublicId);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogCritical("⚠️ [CRITICAL] Rollback sırasında dosya silinemedi! Manuel müdahale gerekebilir: {key}. Hata: {err}", result.PublicId, deleteEx.Message);
                    }
                }
            
                _logger.LogWarning("✅ [Rollback] Temizlik operasyonu tamamlandı. Ortam tertemiz!");
            }
            throw;
        }
    }
}