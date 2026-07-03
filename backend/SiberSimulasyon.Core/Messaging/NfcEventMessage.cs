namespace SiberSimulasyon.Core.Messaging;

public class NfcEventMessage
{
    public string PersonId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
