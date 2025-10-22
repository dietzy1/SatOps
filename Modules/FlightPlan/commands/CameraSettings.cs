using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.FlightPlan.Commands
{
    /// <summary>
    /// Camera configuration settings for image capture
    /// </summary>
    public class CameraSettings
    {
        [Required(ErrorMessage = "CameraId is required")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "CameraId must be between 1 and 128 characters")]
        [JsonPropertyName("cameraId")]
        public string CameraId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        [JsonPropertyName("type")]
        public CameraType Type { get; set; }

        [Required(ErrorMessage = "ExposureMicroseconds is required")]
        [Range(0, 2_000_000, ErrorMessage = "ExposureMicroseconds must be between 0 and 2,000,000")]
        [JsonPropertyName("exposureMicroseconds")]
        public int ExposureMicroseconds { get; set; }

        [Required(ErrorMessage = "Iso is required")]
        [Range(0.1, 10.0, ErrorMessage = "Iso must be between 0.1 and 10.0")]
        [JsonPropertyName("iso")]
        public double Iso { get; set; }

        [Required(ErrorMessage = "NumImages is required")]
        [Range(1, 1000, ErrorMessage = "NumImages must be between 1 and 1000")]
        [JsonPropertyName("numImages")]
        public int NumImages { get; set; }

        [Required(ErrorMessage = "IntervalMicroseconds is required")]
        [Range(0, 60_000_000, ErrorMessage = "IntervalMicroseconds must be between 0 and 60,000,000")]
        [JsonPropertyName("intervalMicroseconds")]
        public int IntervalMicroseconds { get; set; }

        [Required(ErrorMessage = "ObservationId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ObservationId must be a positive integer")]
        [JsonPropertyName("observationId")]
        public int ObservationId { get; set; }

        [Required(ErrorMessage = "PipelineId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PipelineId must be a positive integer")]
        [JsonPropertyName("pipelineId")]
        public int PipelineId { get; set; }
    }

    /// <summary>
    /// Camera types supported by the satellite
    /// </summary>
    public enum CameraType
    {
        VMB = 0,
        IR = 1,
        Test = 2
    }
}
