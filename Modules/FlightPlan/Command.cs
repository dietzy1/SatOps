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

    public abstract class Command : ICommand
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        [JsonPropertyName("commandType")]

        public abstract string CommandType { get; }

        public abstract ValidationResult Validate();
        public abstract Task<List<string>> CompileToCsh();

        public virtual string ToJson()
        {
            var options = JsonSerializerOptionsFactory.Create();
            return JsonSerializer.Serialize(this, this.GetType(), options);
        }

        public static class JsonConfig
        {
            public static JsonSerializerOptions CreateOptions()
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
                };

                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                return options;
            }
        }
    }

    public class CommandJsonConverter : JsonConverter<Command>
    {
        public override Command? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("Command must be a JSON object");
                }

                if (!root.TryGetProperty("commandType", out var commandTypeElement))
                {
                    throw new JsonException("Missing required 'commandType' property");
                }

                if (commandTypeElement.ValueKind != JsonValueKind.String)
                {
                    throw new JsonException("Property 'commandType' must be a string");
                }

                var commandType = commandTypeElement.GetString();

                if (string.IsNullOrWhiteSpace(commandType))
                {
                    throw new JsonException("Property 'commandType' cannot be null or empty");
                }

                try
                {
                    return commandType switch
                    {
                        "triggerCapture" => JsonSerializer.Deserialize<TriggerCaptureCommand>(root.GetRawText(), options),
                        "triggerPipeline" => JsonSerializer.Deserialize<TriggerPipelineCommand>(root.GetRawText(), options),
                        _ => throw new JsonException($"Unknown commandType '{commandType}'. Supported types: 'triggerCapture', 'triggerPipeline'")
                    };
                }
                catch (JsonException)
                {
                    throw; // Re-throw JsonExceptions as-is
                }
                catch (Exception ex)
                {
                    throw new JsonException($"Failed to deserialize command of type '{commandType}': {ex.Message}", ex);
                }
            }
            catch (JsonException)
            {
                throw; // Re-throw JsonExceptions as-is for 400 responses
            }
            catch (Exception ex)
            {
                throw new JsonException($"Invalid JSON format for command: {ex.Message}", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, Command value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }

    public class CommandSequence
    {
        public List<Command> Commands { get; set; } = new();

        public void AddCommand(Command command) => Commands.Add(command);

        public (bool IsValid, List<string> Errors) ValidateAll()
        {
            var errors = new List<string>();

            foreach (var command in Commands)
            {
                var result = command.Validate();
                if (result != ValidationResult.Success)
                {
                    errors.Add($"{command.Name}: {result.ErrorMessage}");
                }
            }

            return (errors.Count == 0, errors);
        }

        public async Task<List<string>> CompileAllToCsh()
        {
            var allCommands = new List<string>();

            foreach (var command in Commands)
            {
                var commandCsh = await command.CompileToCsh();
                allCommands.AddRange(commandCsh);
            }

            return allCommands;
        }

        // Serialize just the Commands list (flat structure)
        public string ToJson()
        {
            var options = JsonSerializerOptionsFactory.Create();
            return JsonSerializer.Serialize(Commands, options);
        }

        // Deserialize from flat Commands array
        public static CommandSequence? FromJson(string json)
        {
            var options = JsonSerializerOptionsFactory.Create();
            var commands = JsonSerializer.Deserialize<List<Command>>(json, options);
            return commands != null ? new CommandSequence { Commands = commands } : null;
        }

        public JsonDocument ToJsonDocument()
        {
            var json = ToJson();
            return JsonDocument.Parse(json);
        }

        public static CommandSequence FromJsonDocument(JsonDocument document)
        {
            var json = document.RootElement.GetRawText();
            return FromJson(json) ?? new CommandSequence();
        }

        public static CommandSequence FromJsonElement(JsonElement element)
        {
            var json = element.GetRawText();
            return FromJson(json) ?? new CommandSequence();
        }
    }

    public static class JsonSerializerOptionsFactory
    {
        public static JsonSerializerOptions Create(bool writeIndented = false)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            options.Converters.Add(new CommandJsonConverter());
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            return options;
        }
    }
}