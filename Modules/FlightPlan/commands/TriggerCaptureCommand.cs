using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SatOps.Modules.FlightPlan.Commands
{
    /// <summary>
    /// Command to trigger image capture on the satellite camera.
    /// The execution time is automatically calculated based on the target location and satellite orbit.
    /// </summary>
    public class TriggerCaptureCommand : Command
    {
        public override string CommandType => CommandTypeConstants.TriggerCapture;

        public override bool RequiresExecutionTimeCalculation => true;

        // Hardcoded CSP node address for the Camera Controller.
        private const int CameraControllerNode = 5422;

        [Required(ErrorMessage = "CaptureLocation is required")]
        [JsonPropertyName("captureLocation")]
        public CaptureLocation? CaptureLocation { get; set; }

        [Required(ErrorMessage = "CameraSettings is required")]
        [JsonPropertyName("cameraSettings")]
        public CameraSettings? CameraSettings { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var result in base.Validate(validationContext))
            {
                yield return result;
            }

            if (CameraSettings != null)
            {
                if (CameraSettings.NumImages > 1 && CameraSettings.IntervalMicroseconds == 0)
                {
                    yield return new ValidationResult(
                        "IntervalMicroseconds must be greater than 0 when capturing multiple images",
                        [nameof(CameraSettings.IntervalMicroseconds)]
                    );
                }

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
                    "ExecutionTime must be calculated before compiling TriggerCaptureCommand.");
            }

            if (CameraSettings == null)
            {
                throw new InvalidOperationException("CameraSettings is required for compilation.");
            }

            if (!Regex.IsMatch(CameraSettings.CameraId, @"^[a-zA-Z0-9\s\-_]+$"))
            {
                throw new InvalidOperationException($"Invalid Camera ID format: {CameraSettings.CameraId}");
            }

            var script = new List<string>
            {
                // NOTE: In the C++ code, setting this parameter automatically sets camera_state_param to 0 (OFF).
                $"set camera_id_param \"{CameraSettings.CameraId}\" -n {CameraControllerNode}",

                // Configure capture parameters
                $"set camera_type_param {(int)CameraSettings.Type} -n {CameraControllerNode}",
                $"set exposure_param {CameraSettings.ExposureMicroseconds} -n {CameraControllerNode}",
                $"set iso_param {CameraSettings.Iso.ToString(CultureInfo.InvariantCulture)} -n {CameraControllerNode}",
                $"set num_images_param {CameraSettings.NumImages} -n {CameraControllerNode}",
                $"set interval_param {CameraSettings.IntervalMicroseconds} -n {CameraControllerNode}",
                $"set obid_param {CameraSettings.ObservationId} -n {CameraControllerNode}",
                $"set pipeline_id_param {CameraSettings.PipelineId} -n {CameraControllerNode}",

                // Power ON the camera
                $"set camera_state_param 1 -n {CameraControllerNode}",

                // Wait for camera boot
                // Vimba cameras need time to enumerate on USB after GPIO power-up.
                "sleep 5",

                // Trigger Capture
                // Setting this to 1 invokes the callback that reads all the above params
                $"set capture_param 1 -n {CameraControllerNode}"
            };

            return Task.FromResult(script);
        }
    }
}