using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.FlightPlan.Commands
{
    /// <summary>
    /// Command to trigger the image processing pipeline on the satellite
    /// </summary>
    public class TriggerPipelineCommand : Command
    {
        public override string CommandType => CommandTypeConstants.TriggerPipeline;

        private const int DippNode = 162;

        [Required(ErrorMessage = "Mode is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Mode must be a non-negative integer")]
        [JsonPropertyName("mode")]
        public int? Mode { get; set; }

        public override Task<List<string>> CompileToCsh()
        {
            var commandString = $"set pipeline_run {Mode!.Value} -n {DippNode}";
            return Task.FromResult(new List<string> { commandString });
        }

        // No custom validation needed beyond data annotations
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}