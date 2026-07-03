namespace SiberSimulasyon.Core.Messaging;

public class CameraStatusMessage
{
    public string CameraId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
