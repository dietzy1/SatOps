using System.ComponentModel.DataAnnotations;
using SatOps.Modules.FlightPlan;

namespace SatOps.Modules.Schedule
{
    public class FlightPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GsId { get; set; }
        public int SatId { get; set; }
        public int? OverpassId { get; set; }
        public int? PreviousPlanId { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public List<Command> Commands { get; set; } = [];

        public string Status { get; set; } = string.Empty;
        public int? ApproverId { get; set; }
        public int CreatedById { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

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
        public List<Command> Commands { get; set; } = new();

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


                    var commandResults = command.Validate(commandContext);

                    foreach (var result in commandResults)
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

    public class ApproveFlightPlanDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class AssociateOverpassDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Optional: Maximum elevation for more precise matching (in degrees)
        public double? MaxElevation { get; set; }

        // Optional: Duration in seconds for additional validation
        public int? DurationSeconds { get; set; }

        // Optional: Maximum elevation time for exact overpass identification
        public DateTime? MaxElevationTime { get; set; }
    }

}