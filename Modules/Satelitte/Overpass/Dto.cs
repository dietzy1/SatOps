using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.Satellite.Overpass
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

    public class OverpassCalculationRequestDto
    {
        [Required]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public double GroundStationLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        public double GroundStationLongitude { get; set; }

        [Range(0, 10000, ErrorMessage = "Altitude must be between 0 and 10,000 km")]
        public double? GroundStationAltitudeKm { get; set; }

        public DateTime? ObservationTime { get; set; }
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
