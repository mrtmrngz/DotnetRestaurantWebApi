namespace RestaurantApi.Application.Features.Files.Dtos;

public class UploadFileResult
{
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public long Size { get; set; }
    public string FileType { get; set; } = null!;
    public string? FileExtension { get; set; }
}