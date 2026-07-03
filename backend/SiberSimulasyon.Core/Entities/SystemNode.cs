namespace SiberSimulasyon.Core.Entities;

public class SystemNode
{
    public int Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}
