namespace SiberSimulasyon.Core.Entities;

public class CameraFeed
{
    public int Id { get; set; }
    public string CameraId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
