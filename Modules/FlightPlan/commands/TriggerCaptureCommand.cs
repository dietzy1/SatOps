using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.FlightPlan.Commands
{
    /// <summary>
    /// Command to trigger image capture on the satellite camera
    /// </summary>
    public class TriggerCaptureCommand : Command
    {
        public override string CommandType => CommandTypeConstants.TriggerCapture;

        // Hardcoded CSP node address for the Camera Controller.
        // TODO: Find out if this is the correct node address. Might be in systemd.
        private const int CameraControllerNode = 2;

        [Required(ErrorMessage = "CameraId is required")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "CameraId must be between 1 and 128 characters")]
        [JsonPropertyName("cameraId")]
        public string? CameraId { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [JsonPropertyName("type")]
        public CameraType? Type { get; set; }

        [Required(ErrorMessage = "ExposureMicroseconds is required")]
        [Range(0, 2_000_000, ErrorMessage = "ExposureMicroseconds must be between 0 and 2,000,000")]
        [JsonPropertyName("exposureMicroseconds")]
        public int? ExposureMicroseconds { get; set; }

        [Required(ErrorMessage = "Iso is required")]
        [Range(0.1, 10.0, ErrorMessage = "Iso must be between 0.1 and 10.0")]
        [JsonPropertyName("iso")]
        public double? Iso { get; set; }

        [Required(ErrorMessage = "NumImages is required")]
        [Range(1, 1000, ErrorMessage = "NumImages must be between 1 and 1000")]
        [JsonPropertyName("numImages")]
        public int? NumImages { get; set; }

        [Required(ErrorMessage = "IntervalMicroseconds is required")]
        [Range(0, 60_000_000, ErrorMessage = "IntervalMicroseconds must be between 0 and 60,000,000")]
        [JsonPropertyName("intervalMicroseconds")]
        public int? IntervalMicroseconds { get; set; }

        [Required(ErrorMessage = "ObservationId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ObservationId must be a positive integer")]
        [JsonPropertyName("observationId")]
        public int? ObservationId { get; set; }

        [Required(ErrorMessage = "PipelineId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PipelineId must be a positive integer")]
        [JsonPropertyName("pipelineId")]
        public int? PipelineId { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Custom validation: if capturing multiple images, interval must be > 0
            if (NumImages.HasValue && IntervalMicroseconds.HasValue && NumImages > 1 && IntervalMicroseconds == 0)
            {
                yield return new ValidationResult(
                    "IntervalMicroseconds must be greater than 0 when capturing multiple images",
                    new[] { nameof(IntervalMicroseconds) }
                );
            }
        }

        public override Task<List<string>> CompileToCsh()
        {
            var script = new List<string>
            {
                $"set camera_id_param \"{CameraId}\" -n {CameraControllerNode}",
                $"set camera_type_param {(int)Type!.Value} -n {CameraControllerNode}",
                $"set exposure_param {ExposureMicroseconds!.Value} -n {CameraControllerNode}",
                $"set iso_param {Iso!.Value} -n {CameraControllerNode}",
                $"set num_images_param {NumImages!.Value} -n {CameraControllerNode}",
                $"set interval_param {IntervalMicroseconds!.Value} -n {CameraControllerNode}",
                $"set obid_param {ObservationId!.Value} -n {CameraControllerNode}",
                $"set pipeline_id_param {PipelineId!.Value} -n {CameraControllerNode}",
                $"set capture_param 1 -n {CameraControllerNode}"
            };

            return Task.FromResult(script);
        }
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