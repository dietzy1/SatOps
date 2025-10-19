using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SatOps.Modules.FlightPlan.Commands;

namespace SatOps.Modules.FlightPlan
{
    public enum CommandType
    {
        TriggerCapture,
        TriggerPipeline
    }

    public static class CommandTypeConstants
    {
        public const string TriggerCapture = "TRIGGER_CAPTURE";
        public const string TriggerPipeline = "TRIGGER_PIPELINE";
    }

    public interface ICompilableCommand
    {
        Task<List<string>> CompileToCsh();
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "commandType")]
    [JsonDerivedType(typeof(TriggerCaptureCommand), typeDiscriminator: CommandTypeConstants.TriggerCapture)]
    [JsonDerivedType(typeof(TriggerPipelineCommand), typeDiscriminator: CommandTypeConstants.TriggerPipeline)]
    public class Command : IValidatableObject, ICompilableCommand
    {
        public virtual CommandType CommandType { get; }

        [Required]
        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // No custom validation needed beyond DataAnnotations
            // Just return empty - ASP.NET Core handles DataAnnotations automatically
            yield break;
        }

        public virtual Task<List<string>> CompileToCsh()
        {
            return Task.FromResult(new List<string>());
        }
    }

    public class CommandSequence
    {
        [JsonPropertyName("commands")]
        public List<Command> Commands { get; set; } = new();

        public void AddCommand(Command command) => Commands.Add(command);

        public List<ValidationResult> ValidateAll()
        {
            var results = new List<ValidationResult>();
            foreach (var cmd in Commands)
            {
                var context = new ValidationContext(cmd);
                Validator.TryValidateObject(cmd, context, results, validateAllProperties: true);
            }
            return results;
        }
    }
}