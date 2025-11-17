using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.GroundStationLink
{

    public class WebSocketConnectMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class WebSocketScheduleTransmissionMessage
    {
        [JsonPropertyName("requestId")]
        public Guid RequestId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("frames")]
        public int Frames { get; set; }

        [JsonPropertyName("data")]
        public WebSocketScheduleTransmissionData Data { get; set; } = new();
    }

    public class WebSocketScheduleTransmissionData
    {
        [JsonPropertyName("satellite")]
        public string Satellite { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("flightPlanId")]
        public int FlightPlanId { get; set; }

        [JsonPropertyName("satelliteId")]
        public int SatelliteId { get; set; }

        [JsonPropertyName("groundStationId")]
        public int GroundStationId { get; set; }
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
    }

    /// <summary>
    /// Response DTO for image retrieval endpoint
    /// </summary>
    public class ImageResponseDto
    {
        [JsonPropertyName("imageId")]
        public int ImageId { get; set; }

        [JsonPropertyName("flightPlanId")]
        public int? FlightPlanId { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("captureTime")]
        public DateTime CaptureTime { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }
    }
}