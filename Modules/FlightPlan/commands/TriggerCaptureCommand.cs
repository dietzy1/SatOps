using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.FlightPlan.Commands
{
    public enum CameraType
    {
        VMB = 0,
        IR = 1,
        Test = 2
    }

    public class TriggerCaptureCommand : Command
    {
        public override CommandType CommandType => CommandType.TriggerCapture;
        private const int CameraControllerNode = 2;

        [Required(ErrorMessage = "CameraId is required")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "CameraId must be between 1 and 128 characters")]
        public string CameraId { get; set; } = "1800 U-500c";

        [Required(ErrorMessage = "Camera type is required")]
        public CameraType Type { get; set; } = CameraType.VMB;

        [Range(0, 2_000_000, ErrorMessage = "Exposure must be between 0 and 2,000,000 microseconds")]
        public int ExposureMicroseconds { get; set; } = 55000;

        [Range(0.1, 10.0, ErrorMessage = "ISO must be between 0.1 and 10.0")]
        public double Iso { get; set; } = 1.0;

        [Range(1, 1000, ErrorMessage = "Number of images must be between 1 and 1000")]
        public int NumImages { get; set; } = 1;

        [Range(0, 60_000_000, ErrorMessage = "Interval must be between 0 and 60,000,000 microseconds")]
        public int IntervalMicroseconds { get; set; } = 0;

        [Range(1, int.MaxValue, ErrorMessage = "ObservationId must be a positive integer")]
        public int ObservationId { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "PipelineId must be a positive integer")]
        public int PipelineId { get; set; } = 1;

        // REMOVE the old Validate() method entirely
        // REPLACE with this IValidatableObject implementation:
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // DataAnnotations are automatically validated by ASP.NET Core
            // We only need to add custom business logic here

            if (NumImages > 1 && IntervalMicroseconds == 0)
            {
                yield return new ValidationResult(
                    "Interval must be greater than 0 when capturing multiple images",
                    new[] { nameof(IntervalMicroseconds), nameof(NumImages) }
                );
            }

            // No need to return anything else - if no issues, method completes
        }

        public override Task<List<string>> CompileToCsh()
        {
            var script = new List<string>
            {
                $"set camera_id_param \"{CameraId}\" -n {CameraControllerNode}",
                $"set camera_type_param {(int)Type} -n {CameraControllerNode}",
                $"set exposure_param {ExposureMicroseconds} -n {CameraControllerNode}",
                $"set iso_param {Iso} -n {CameraControllerNode}",
                $"set num_images_param {NumImages} -n {CameraControllerNode}",
                $"set interval_param {IntervalMicroseconds} -n {CameraControllerNode}",
                $"set obid_param {ObservationId} -n {CameraControllerNode}",
                $"set pipeline_id_param {PipelineId} -n {CameraControllerNode}",
                $"set capture_param 1 -n {CameraControllerNode}"
            };

            return Task.FromResult(script);
        }
    }
}