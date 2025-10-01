using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.Operation
{
    // Telemetry DTOs
    public class TelemetryDataReceiveDto
    {
        [Required]
        public int GroundStationId { get; set; }

        [Required]
        public int SatelliteId { get; set; }

        [Required]
        public int FlightPlanId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public IFormFile Data { get; set; } = null!;
    }

    // Image DTOs
    public class ImageDataReceiveDto
    {
        [Required]
        public int SatelliteId { get; set; }

        [Required]
        public int GroundStationId { get; set; }

        public int FlightPlanId { get; set; }

        [Required]
        public DateTime CaptureTime { get; set; }

        [Required]
        public IFormFile ImageFile { get; set; } = null!;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Metadata { get; set; }
    }
}
