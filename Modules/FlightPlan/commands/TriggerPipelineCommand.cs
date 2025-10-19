using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.FlightPlan.Commands
{
    public class TriggerPipelineCommand : Command
    {
        public override CommandType CommandType => CommandType.TriggerPipeline;
        private const int DippNode = 162;

        [Range(0, 100, ErrorMessage = "Mode must be between 0 and 100")]
        public int Mode { get; set; } = 1;

        // REMOVE: public override ValidationResult Validate() => ValidationResult.Success!;

        // REPLACE with this:
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // No custom validation needed beyond DataAnnotations
            // Just return empty - ASP.NET Core handles DataAnnotations automatically
            yield break;
        }

        public override Task<List<string>> CompileToCsh()
        {
            var commandString = $"set pipeline_run {Mode} -n {DippNode}";
            return Task.FromResult(new List<string> { commandString });
        }
    }
}