namespace SiberSimulasyon.Core.Messaging;

public class SystemHeartbeatMessage
{
    public string Hostname { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
