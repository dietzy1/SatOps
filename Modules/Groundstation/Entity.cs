using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatOps.Modules.Groundstation
{

    [Table("ground_stations")]
    public class GroundStation
    {

        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Location Location { get; set; } = null!;

        [Required]
        public string HttpUrl { get; set; } = string.Empty;

        [Required]
        public Guid ApplicationId { get; set; } = Guid.NewGuid();

        [Required]
        public string ApiKeyHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = false; // Maybe rename to IsHealthy or similar

        // Inverse Navigation Properties
        public virtual ICollection<FlightPlan.FlightPlan> FlightPlans { get; set; } = new List<FlightPlan.FlightPlan>();
        public virtual ICollection<Overpass.Entity> Overpasses { get; set; } = new List<Overpass.Entity>();
        public virtual ICollection<Operation.ImageData> Images { get; set; } = new List<Operation.ImageData>();
        public virtual ICollection<Operation.TelemetryData> Telemetry { get; set; } = new List<Operation.TelemetryData>();

    }

    public class Location
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double Altitude { get; set; } = 0;
    }
}