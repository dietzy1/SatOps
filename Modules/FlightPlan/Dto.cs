using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.FlightPlan
{
    public class FlightPlanDto
    {
        public int Id { get; set; }
        public int? PreviousPlanId { get; set; }
        public int GsId { get; set; }
        public int SatId { get; set; }
        public int? OverpassId { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public List<Command> Commands { get; set; } = [];
        public string Status { get; set; } = string.Empty;
        public int CreatedById { get; set; }
        public int? ApprovedById { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new flight plan
    /// </summary>
    public class CreateFlightPlanDto : IValidatableObject
    {
        [Required(ErrorMessage = "Ground station ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Ground station ID must be positive")]
        public int GsId { get; set; }

        [Required(ErrorMessage = "Satellite ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Satellite ID must be positive")]
        public int SatId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Commands list is required")]
        [MinLength(1, ErrorMessage = "At least one command is required")]
        public List<Command> Commands { get; set; } = [];

        /// <summary>
        /// Validates the entire DTO including all commands
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Commands != null && Commands.Any())
            {
                for (int i = 0; i < Commands.Count; i++)
                {
                    var command = Commands[i];
                    var commandContext = new ValidationContext(command);
                    var commandResults = new List<ValidationResult>();

                    // Validate data annotations
                    bool isValid = Validator.TryValidateObject(command, commandContext, commandResults, validateAllProperties: true);

                    foreach (var result in commandResults)
                    {
                        yield return new ValidationResult(
                            $"Command {i + 1} ({command.CommandType}): {result.ErrorMessage}",
                            result.MemberNames.Select(m => $"Commands[{i}].{m}")
                        );
                    }

                    // Validate custom logic via IValidatableObject
                    var customResults = command.Validate(commandContext);
                    foreach (var result in customResults)
                    {
                        if (result != ValidationResult.Success)
                        {
                            yield return new ValidationResult(
                                $"Command {i + 1} ({command.CommandType}): {result.ErrorMessage}",
                                result.MemberNames.Select(m => $"Commands[{i}].{m}")
                            );
                        }
                    }
                }
            }
        }
    }

    // DTO for the PATCH (approve/reject) request body
    public class ApproveFlightPlanDto
    {
        public string Status { get; set; } = string.Empty;
    }

    // DTO for associating with overpass using timerange and optional matching criteria
    public class AssignOverpassDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double? MaxElevation { get; set; }
        public int? DurationSeconds { get; set; }
        public DateTime? MaxElevationTime { get; set; }
    }

    public class ImagingTimingRequestDto
    {
        public int SatelliteId { get; set; }
        public DateTime? CommandReceptionTime { get; set; }
        public double TargetLatitude { get; set; }
        public double TargetLongitude { get; set; }

        // Off-nadir imaging parameters (replaces fixed MaxDistanceKm)
        public double MaxOffNadirDegrees { get; set; } = 10.0; // Default 10� off-nadir maximum

        // Search parameters
        public int MaxSearchDurationHours { get; set; } = 48; // Search up to 48 hours ahead
    }

    public class ImagingTimingResponseDto
    {
        public DateTime? ImagingTime { get; set; }
        public double? OffNadirDegrees { get; set; }
        public double? SatelliteAltitudeKm { get; set; }

        // TLE age warning
        public bool TleAgeWarning { get; set; }
        public double? TleAgeHours { get; set; }

        public string? Message { get; set; }
    }
}