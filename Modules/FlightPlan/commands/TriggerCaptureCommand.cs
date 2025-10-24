using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.FlightPlan.Commands
{
    /// <summary>
    /// Command to trigger image capture on the satellite camera.
    /// The execution time is automatically calculated based on the target location and satellite orbit.
    /// </summary>
    public class TriggerCaptureCommand : Command
    {
        public override string CommandType => CommandTypeConstants.TriggerCapture;

        /// <summary>
        /// This command requires execution time calculation based on imaging opportunities
        /// </summary>
        public override bool RequiresExecutionTimeCalculation => true;

        // Hardcoded CSP node address for the Camera Controller.
        // TODO: Find out if this is the correct node address. Might be in systemd.
        private const int CameraControllerNode = 2;

        /// <summary>
        /// Geographic coordinates where the image should be captured.
        /// The execution time will be calculated based on this location and the satellite's orbit.
        /// </summary>
        [Required(ErrorMessage = "CaptureLocation is required")]
        [JsonPropertyName("captureLocation")]
        public CaptureLocation? CaptureLocation { get; set; }

        /// <summary>
        /// Camera configuration settings for the image capture
        /// </summary>
        [Required(ErrorMessage = "CameraSettings is required")]
        [JsonPropertyName("cameraSettings")]
        public CameraSettings? CameraSettings { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Call base validation (checks ExecutionTime requirements)
            foreach (var result in base.Validate(validationContext))
            {
                yield return result;
            }

            // Validate nested CameraSettings
            if (CameraSettings != null)
            {
                // Custom validation: if capturing multiple images, interval must be > 0
                if (CameraSettings.NumImages > 1 && CameraSettings.IntervalMicroseconds == 0)
                {
                    yield return new ValidationResult(
                        "IntervalMicroseconds must be greater than 0 when capturing multiple images",
                        new[] { nameof(CameraSettings.IntervalMicroseconds) }
                    );
                }

                // Validate the CameraSettings object
                var context = new ValidationContext(CameraSettings);
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(CameraSettings, context, results, validateAllProperties: true))
                {
                    foreach (var result in results)
                    {
                        yield return result;
                    }
                }
            }

            // Validate CaptureLocation
            if (CaptureLocation != null)
            {
                var context = new ValidationContext(CaptureLocation);
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(CaptureLocation, context, results, validateAllProperties: true))
                {
                    foreach (var result in results)
                    {
                        yield return result;
                    }
                }
            }
        }

        public override Task<List<string>> CompileToCsh()
        {
            // ExecutionTime must be set before compilation (via CalculateExecutionTimes)
            if (!ExecutionTime.HasValue)
            {
                throw new InvalidOperationException(
                    "ExecutionTime must be calculated before compiling TriggerCaptureCommand. " +
                    "Ensure CalculateExecutionTimes() is called before CompileToCsh().");
            }

            if (CameraSettings == null)
            {
                throw new InvalidOperationException("CameraSettings is required for TriggerCaptureCommand compilation.");
            }

            //TODO: We need to figure out how to sleep/wait until ExecutionTime before running the capture commands
            // Then we add that stuff here infront :)

            var script = new List<string>
            {
                $"set camera_id_param \"{CameraSettings.CameraId}\" -n {CameraControllerNode}",
                $"set camera_type_param {(int)CameraSettings.Type} -n {CameraControllerNode}",
                $"set exposure_param {CameraSettings.ExposureMicroseconds} -n {CameraControllerNode}",
                $"set iso_param {CameraSettings.Iso} -n {CameraControllerNode}",
                $"set num_images_param {CameraSettings.NumImages} -n {CameraControllerNode}",
                $"set interval_param {CameraSettings.IntervalMicroseconds} -n {CameraControllerNode}",
                $"set obid_param {CameraSettings.ObservationId} -n {CameraControllerNode}",
                $"set pipeline_id_param {CameraSettings.PipelineId} -n {CameraControllerNode}",
                $"set capture_param 1 -n {CameraControllerNode}"
            };

            return Task.FromResult(script);
        }
    }
}
