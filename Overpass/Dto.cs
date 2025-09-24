using System.ComponentModel.DataAnnotations;

namespace SatOps.Overpass
{
    public class SatelliteObservationDto
    {
        public int SatelliteId { get; set; }
        public string SatelliteName { get; set; } = string.Empty;
        public int? GroundStationId { get; set; }
        public string? GroundStationName { get; set; }
        public DateTime ObservationTime { get; set; }
        public double GroundStationLatitude { get; set; }
        public double GroundStationLongitude { get; set; }
        public double GroundStationAltitudeKm { get; set; }
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public double Range { get; set; }
        public double RangeRate { get; set; }
        public bool IsVisible { get; set; }
    }

    public class OverpassWindowsCalculationRequestDto
    {
        [Required]
        public int SatelliteId { get; set; }
        [Required]
        public int GroundStationId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double MinimumElevation { get; set; } = 0.0;
    }

    public class OverpassTimeRequestDto
    {
        public DateTime? ObservationTime { get; set; }
    }

    public class OverpassTimeWindowRequestDto
    {
        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Range(0, 90, ErrorMessage = "Minimum elevation must be between 0 and 90 degrees")]
        public double MinimumElevation { get; set; } = 0.0;
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
        public double Duration { get; set; } // in seconds
        public double StartAzimuth { get; set; }
        public double EndAzimuth { get; set; }
    }

    public class NextOverpassRequestDto
    {
        public DateTime? FromTime { get; set; }

        [Range(0, 90, ErrorMessage = "Minimum elevation must be between 0 and 90 degrees")]
        public double MinimumElevation { get; set; } = 0.0;
    }
}
