using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SatOps.Modules.FlightPlan
{
    public class TriggerCaptureCommand : Command
    {
        public override string Name => "Trigger Camera Capture";
        public override string Description => "Commands the satellite to capture one or more images and send them to the processing pipeline.";
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
            var payloadBuilder = new StringBuilder();
            payloadBuilder.Append($"CAMERA_TYPE={Type.ToString().ToUpper()};");
            payloadBuilder.Append($"CAMERA_ID={CameraId};");
            payloadBuilder.Append($"NUM_IMAGES={NumImages};");
            payloadBuilder.Append($"EXPOSURE={ExposureMicroseconds};");
            payloadBuilder.Append($"ISO={Iso};");
            payloadBuilder.Append($"INTERVAL={IntervalMicroseconds};");
            payloadBuilder.Append($"PIPELINE_ID={PipelineId};");
            payloadBuilder.Append($"OBID={ObservationId};");

            string commandString = $"param set capture_param \"{payloadBuilder.ToString()}\" -n {CameraControllerNode}";

            return Task.FromResult(new List<string> { commandString });
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