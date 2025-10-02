using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.FlightPlan.Commands
{
    public class TriggerCaptureCommand : Command
    {
        public override string Name => "Trigger Camera Capture";
        public override string Description => "Configures and triggers the satellite's camera controller.";
        public override string CommandType => "triggerCapture";

        /// Hardcoded CSP node address for the Camera Controller.
        private const int CameraControllerNode = 2;

        [Required]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "CameraId must be a valid string.")]
        public string CameraId { get; set; } = "1800 U-500c";

        [Required]
        public CameraType Type { get; set; } = CameraType.VMB;

        [Range(0, 2_000_000, ErrorMessage = "Exposure must be between 0 and 2,000,000 microseconds.")]
        public int ExposureMicroseconds { get; set; } = 55000;

        [Range(0.1, 10.0, ErrorMessage = "ISO must be between 0.1 and 10.0.")]
        public double Iso { get; set; } = 1.0;

        [Range(1, 1000, ErrorMessage = "Number of images must be between 1 and 1000.")]
        public int NumImages { get; set; } = 1;

        [Range(0, 60_000_000, ErrorMessage = "Interval must be a positive number of microseconds.")]
        public int IntervalMicroseconds { get; set; } = 0;

        [Range(1, int.MaxValue, ErrorMessage = "ObservationId must be a positive integer.")]
        public int ObservationId { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "PipelineId must be a positive integer.")]
        public int PipelineId { get; set; } = 1;


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


        public override ValidationResult Validate()
        {
            var validationContext = new ValidationContext(this);
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(this, validationContext, results, true);

            if (!isValid)
            {
                string errorMessages = string.Join("; ", results.Select(r => r.ErrorMessage));
                return new ValidationResult(errorMessages);
            }

            if (NumImages > 1 && IntervalMicroseconds == 0)
            {
                return new ValidationResult("Interval must be greater than 0 when capturing multiple images.");
            }

            return ValidationResult.Success!;
        }
    }

    public enum CameraType
    {
        VMB = 0,
        IR = 1,
        Test = 2
    }
}