using System;

namespace SiberSimulasyon.Core.Entities
{
    public class NfcLog
    {
        public int Id { get; set; }
        public string PersonId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}