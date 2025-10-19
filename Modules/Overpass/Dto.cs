using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.Overpass
{
    public class OverpassWindowsCalculationRequestDto
    {
        [Required]
        public int SatelliteId { get; set; }
        [Required]
        public int GroundStationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [Range(0, 90, ErrorMessage = "Minimum elevation must be between 0 and 90 degrees")]
        public double MinimumElevation { get; set; } = 0.0;
        public int? MaxResults { get; set; }
        public int? MinimumDurationSeconds { get; set; }
    }

    public class OverpassWindowDto
    {
        public int SatelliteId { get; set; }
        public string SatelliteName { get; set; } = string.Empty;
        public int GroundStationId { get; set; }
        public string GroundStationName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime MaxElevationTime { get; set; }
        public double MaxElevation { get; set; }
        public double DurationSeconds { get; set; }
        public double StartAzimuth { get; set; }
        public double EndAzimuth { get; set; }

        // Optional flight plan association (when stored overpass has associated flight plan)
        public AssociatedFlightPlanDto? AssociatedFlightPlan { get; set; }

        // TLE data used for calculation (when available from stored overpass)
        public TleDataDto? TleData { get; set; }
    }

    public class AssociatedFlightPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
    }

    public class TleDataDto
    {
        public string TleLine1 { get; set; } = string.Empty;
        public string TleLine2 { get; set; } = string.Empty;
        public DateTime? UpdateTime { get; set; }
    }
}
