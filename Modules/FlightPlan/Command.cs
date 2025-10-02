using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatOps.Modules.FlightPlan.Commands;

namespace SatOps.Modules.FlightPlan
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        string CommandType { get; }

        ValidationResult Validate();

        Task<List<string>> CompileToCsh();

        string ToJson();
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "commandType")]
    [JsonDerivedType(typeof(TriggerCaptureCommand), "triggerCapture")]
    // TODO: Add [JsonDerivedType] for every new command class
    public abstract class Command : ICommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string CommandType { get; }

        public abstract ValidationResult Validate();
        public abstract Task<List<string>> CompileToCsh();

        public virtual string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, this.GetType(), options);
        }
    }

    public class CommandSequence
    {
        public List<Command> Commands { get; set; } = new();

        public void AddCommand(Command command) => Commands.Add(command);

        public List<ValidationResult> ValidateAll() => Commands.Select(cmd => cmd.Validate()).ToList();

        public string ToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return JsonSerializer.Serialize(this, options);
        }

        public static CommandSequence? FromJson(string json)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return JsonSerializer.Deserialize<CommandSequence>(json, options);
        }
    }
}