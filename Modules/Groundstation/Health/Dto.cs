
namespace SatOps.Modules.Groundstation.Health
{
    public class GroundStationHealthDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Status => IsActive ? "Healthy" : "Unhealthy";
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public string CheckType { get; set; } = "Real-time HTTP Health Check";
    }
}