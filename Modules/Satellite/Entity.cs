using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatOps.Modules.Satellite
{
    [Table("satellites")]
    public class Satellite
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        // Core Satellite Information
        [Required]
        [Range(1, 999999999, ErrorMessage = "NoradId must be positive and 1 to 9 digits long")]
        public int NoradId { get; set; }
        public SatelliteStatus Status { get; set; } = SatelliteStatus.Inactive;
        public string TleLine1 { get; set; } = string.Empty;
        public string TleLine2 { get; set; } = string.Empty;

        // Inverse Navigation Properties
        public virtual ICollection<FlightPlan.FlightPlan> FlightPlans { get; set; } = new List<FlightPlan.FlightPlan>();
        public virtual ICollection<Overpass.Entity> Overpasses { get; set; } = new List<Overpass.Entity>();
        public virtual ICollection<Operation.ImageData> Images { get; set; } = new List<Operation.ImageData>();
        public virtual ICollection<Operation.TelemetryData> Telemetry { get; set; } = new List<Operation.TelemetryData>();
    }

    public enum SatelliteStatus
    {
        Active = 0,
        Inactive = 1,
        Deorbited = 2,
        Unknown = 3,
        Decayed = 4,
        Launched = 5
    }
}