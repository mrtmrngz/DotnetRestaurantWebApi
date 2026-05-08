namespace RestaurantApi.Domain.Entities;

public class Media
{
    public Guid Id { get; set; }
    public string Url { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public long Size { get; set; }
    public string FileType { get; set; } = null!;
    public string? FileExtension { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}