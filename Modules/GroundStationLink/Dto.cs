using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.GroundStationLink
{

    public class HelloMessageDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class ScheduleTransmissionMessage
    {
        [JsonPropertyName("request_id")]
        public Guid RequestId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("frames")]
        public int Frames { get; set; }

        [JsonPropertyName("data")]
        public ScheduleTransmissionData Data { get; set; } = new();
    }

    public class ScheduleTransmissionData
    {
        [JsonPropertyName("satellite")]
        public string Satellite { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;
    }

    public class ConnectionStatusDto
    {
        public int GroundStationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public double UptimeMinutes { get; set; }
        public Guid? LastCommandId { get; set; }
    }


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