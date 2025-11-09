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
        public virtual ICollection<GroundStationLink.ImageData> Images { get; set; } = new List<GroundStationLink.ImageData>();
    }

    public enum SatelliteStatus
    {
        Active = 0,
        Inactive = 1,
    }
}