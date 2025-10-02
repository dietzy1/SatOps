using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SatOps.Modules.FlightPlan
{
    public class CameraCommand : Command
    {
        public override string Name => "Camera Capture";
        public override string Description => "Captures images using the specified satellite camera";
        public override string CommandType => "camera";

        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string CameraId { get; set; } = "1800 U-500c";

        [Range(0, 2)]
        public CameraType Type { get; set; } = CameraType.VMB;

        [Range(1, 1_000_000)]
        public int ExposureMicroseconds { get; set; } = 55000;

        [Range(0.1, 10.0)]
        public double Iso { get; set; } = 1.0;

        [Range(1, 1000)]
        public int NumImages { get; set; } = 1;

        [Range(0, 60_000_000)]
        public int IntervalMicroseconds { get; set; } = 0;

        [Range(1, int.MaxValue)]
        public int ObservationId { get; set; } = 1;

        [Range(1, int.MaxValue)]
        public int PipelineId { get; set; } = 1;

        public override ValidationResult Validate()
        {
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(this, context, results, true))
            {
                return new ValidationResult(string.Join("; ", results.Select(r => r.ErrorMessage)));
            }

            if (NumImages > 1 && IntervalMicroseconds == 0)
            {
                return new ValidationResult("Interval must be greater than 0 when capturing multiple images");
            }

            if (!IsValidCameraId(CameraId))
            {
                return new ValidationResult($"Invalid camera ID: {CameraId}. Must be '1800 U-500c', '1800 U-507c', or 'Boson'");
            }

            return ValidationResult.Success!;
        }

        public override string CompileToSatelliteProtocol()
        {
            // Here we gotta convert it into valid CSH (I guess)  instead
            return JsonSerializer.Serialize(new
            {
                command = "camera_capture",
                parameters = new
                {
                    capture_param = 1,
                    camera_id_param = CameraId,
                    camera_type_param = (int)Type,
                    exposure_param = ExposureMicroseconds,
                    iso_param = Iso,
                    num_images_param = NumImages,
                    interval_param = IntervalMicroseconds,
                    obid_param = ObservationId,
                    pipeline_id_param = PipelineId
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        private static bool IsValidCameraId(string cameraId)
        {
            var validIds = new[] { "1800 U-500c", "1800 U-507c", "Boson" };
            return validIds.Contains(cameraId, StringComparer.OrdinalIgnoreCase);
        }
    }

    public enum CameraType
    {
        VMB = 0,
        IR = 1,
        Test = 2
    }
}