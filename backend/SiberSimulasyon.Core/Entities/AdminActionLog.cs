using System;

namespace SiberSimulasyon.Core.Entities
{
    public class AdminActionLog
    {
        public int Id { get; set; }
        public string AdminUsername { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TargetUser { get; set; } = string.Empty;
    }
}