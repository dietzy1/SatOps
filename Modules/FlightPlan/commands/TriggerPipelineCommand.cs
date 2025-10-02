using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.FlightPlan.Commands
{
    public class TriggerPipelineCommand : Command
    {
        public override string Name => "Trigger DIPP Pipeline";
        public override string Description => "Starts the DIPP image processing pipeline.";
        public override string CommandType => "triggerPipeline";

        private const int DippNode = 162;

        public int Mode { get; set; } = 1;

        public override Task<List<string>> CompileToCsh()
        {
            var commandString = $"set pipeline_run {Mode} -n {DippNode}";
            return Task.FromResult(new List<string> { commandString });
        }

        public override ValidationResult Validate() => ValidationResult.Success!;
    }
}