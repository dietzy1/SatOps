using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
                        [nameof(CameraSettings.IntervalMicroseconds)]
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

            if (CaptureLocation == null)
            {
                throw new InvalidOperationException("CaptureLocation is required for TriggerCaptureCommand compilation.");
            }

            // Convert CameraType enum to the string expected by the satellite
            string cameraTypeString = CameraSettings.Type switch
            {
                CameraType.VMB => "VMB",
                CameraType.IR => "IR",
                CameraType.Test => "TEST",
                _ => throw new InvalidOperationException($"Unsupported CameraType: {CameraSettings.Type}")
            };

            // Build the semicolon-delimited string payload
            var commandPayload = new System.Text.StringBuilder();
            commandPayload.Append($"CAMERA_ID={CameraSettings.CameraId};");
            commandPayload.Append($"CAMERA_TYPE={cameraTypeString};");
            commandPayload.Append($"NUM_IMAGES={CameraSettings.NumImages};");
            commandPayload.Append($"EXPOSURE={CameraSettings.ExposureMicroseconds};");
            commandPayload.Append($"ISO={CameraSettings.Iso.ToString(CultureInfo.InvariantCulture)};");
            commandPayload.Append($"INTERVAL={CameraSettings.IntervalMicroseconds};");
            commandPayload.Append($"OBID={CameraSettings.ObservationId};");
            commandPayload.Append($"PIPELINE_ID={CameraSettings.PipelineId};");

            // The satellite code does not parse OBID from this string... but it should.
            // I am including it here assuming the satellite code will be fixed.

            var cshCommand = $"set capture_param \"{commandPayload}\" -n {CameraControllerNode}";

            var script = new List<string> { cshCommand };

            return Task.FromResult(script);
        }
    }
}
