using System.ComponentModel.DataAnnotations.Schema;
using SatelliteModel = SatOps.Modules.Satellite.Satellite;
using SatOps.Modules.Groundstation;

namespace SatOps.Modules.Overpass
{
    [Table("overpasses")]
    public class Entity
    {
        public int Id { get; set; }

        // Foreign key fields
        public int SatelliteId { get; set; }
        public int GroundStationId { get; set; }
        public int FlightPlanId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(SatelliteId))]
        public SatelliteModel Satellite { get; set; } = null!;

        [ForeignKey(nameof(GroundStationId))]
        public GroundStation GroundStation { get; set; } = null!;

        [ForeignKey(nameof(FlightPlanId))]
        public FlightPlan.FlightPlan FlightPlan { get; set; } = null!;

        // Data
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime MaxElevationTime { get; set; }
        public double MaxElevation { get; set; }
        public int DurationSeconds { get; set; }
        public double StartAzimuth { get; set; }
        public double EndAzimuth { get; set; }

        // TLE data
        public string? TleLine1 { get; set; }
        public string? TleLine2 { get; set; }
        public DateTime? TleUpdateTime { get; set; }
    }
}
