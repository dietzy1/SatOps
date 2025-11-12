using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using SatOps.Modules.FlightPlan.Commands;

namespace SatOps.Modules.FlightPlan
{
    /// <summary>
    /// Constants for command type discriminators matching the API contract
    /// </summary>
    public static class CommandTypeConstants
    {
        public const string TriggerCapture = "TRIGGER_CAPTURE";
        public const string TriggerPipeline = "TRIGGER_PIPELINE";
    }

    /// <summary>
    /// Base command class with polymorphic JSON serialization support.
    /// Uses custom JsonConverter for type discrimination and clear error messages.
    /// </summary>
    [JsonConverter(typeof(CommandJsonConverter))]
    public abstract class Command : IValidatableObject
    {
        /// <summary>
        /// The type discriminator for this command (e.g., "TRIGGER_CAPTURE")
        /// </summary>
        [JsonPropertyName("commandType")]
        public abstract string CommandType { get; }

        /// <summary>
        /// When this command should be executed (UTC).
        /// For some commands (like TriggerCaptureCommand), this is calculated automatically
        /// based on target location and satellite orbit. For others, it must be provided by the user.
        /// </summary>
        [JsonPropertyName("executionTime")]
        public DateTime? ExecutionTime { get; set; }

        /// <summary>
        /// Indicates whether this command requires execution time calculation
        /// based on imaging opportunities (e.g., TriggerCaptureCommand).
        /// Commands that override this to return true will have their ExecutionTime
        /// calculated before compilation.
        /// </summary>
        public virtual bool RequiresExecutionTimeCalculation => false;

        /// <summary>
        /// Validates the command using data annotations and custom logic
        /// </summary>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // For commands that don't require execution time calculation, ExecutionTime is required
            if (!RequiresExecutionTimeCalculation && !ExecutionTime.HasValue)
            {
                yield return new ValidationResult(
                    "ExecutionTime is required",
                    [nameof(ExecutionTime)]
                );
            }
            // For commands that do require calculation, ExecutionTime should not be provided by user
            else if (RequiresExecutionTimeCalculation && ExecutionTime.HasValue)
            {
                yield return new ValidationResult(
                    $"ExecutionTime should not be provided for {CommandType}. It will be calculated automatically based on target location.",
                    [nameof(ExecutionTime)]
                );
            }
        }

        /// <summary>
        /// Compiles this command into CSH (Cubesat Space Protocol Shell) commands
        /// </summary>
        public abstract Task<List<string>> CompileToCsh();
    }

    /// <summary>
    /// Custom JSON converter for Command that provides clear error messages for validation failures
    /// </summary>
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
                        CommandTypeConstants.TriggerCapture => JsonSerializer.Deserialize<TriggerCaptureCommand>(root.GetRawText(), options),
                        CommandTypeConstants.TriggerPipeline => JsonSerializer.Deserialize<TriggerPipelineCommand>(root.GetRawText(), options),
                        _ => throw new JsonException($"Unknown commandType '{commandType}'. Supported types: '{CommandTypeConstants.TriggerCapture}', '{CommandTypeConstants.TriggerPipeline}'")
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
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    /// <summary>
    /// Factory for creating JSON serializer options with command serialization support
    /// </summary>
    public static class JsonSerializerOptionsFactory
    {
        public static JsonSerializerOptions Create(bool writeIndented = false)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
            };

            options.Converters.Add(new CommandJsonConverter());
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            return options;
        }
    }

    /// <summary>
    /// Extension methods for working with command lists
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Validates all commands in the list
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateAll(this List<Command> commands)
        {
            var errors = new List<string>();

            for (int i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                var context = new ValidationContext(command);
                var results = new List<ValidationResult>();

                // Validate data annotations
                bool isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);

                if (!isValid)
                {
                    foreach (var result in results)
                    {
                        errors.Add($"Command {i + 1} ({command.CommandType}): {result.ErrorMessage}");
                    }
                }

                // Validate custom logic via IValidatableObject
                var customResults = command.Validate(context);
                foreach (var result in customResults)
                {
                    if (result != ValidationResult.Success)
                    {
                        errors.Add($"Command {i + 1} ({command.CommandType}): {result.ErrorMessage}");
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Calculates execution times for commands that require it (e.g., TriggerCaptureCommand).
        /// This should be called before CompileAllToCsh() for flight plans containing such commands.
        /// </summary>
        public static async Task CalculateExecutionTimesAsync(
            this List<Command> commands,
            Satellite.Satellite satellite,
            IImagingCalculation imagingCalculation,
            DateTime? commandReceptionTime = null)
        {
            if (string.IsNullOrWhiteSpace(satellite.TleLine1) || string.IsNullOrWhiteSpace(satellite.TleLine2))
            {
                throw new InvalidOperationException($"Satellite '{satellite.Name}' does not have valid TLE data.");
            }

            var tle = new SGPdotNET.TLE.Tle(satellite.Name, satellite.TleLine1, satellite.TleLine2);
            var sgp4Satellite = new SGPdotNET.Observation.Satellite(tle);
            var receptionTime = commandReceptionTime ?? DateTime.UtcNow;

            foreach (var command in commands)
            {
                if (command is TriggerCaptureCommand captureCommand && captureCommand.RequiresExecutionTimeCalculation)
                {
                    if (captureCommand.CaptureLocation == null)
                    {
                        throw new InvalidOperationException(
                            "TriggerCaptureCommand requires CaptureLocation to calculate execution time.");
                    }

                    // Create target coordinate
                    var targetCoordinate = new SGPdotNET.CoordinateSystem.GeodeticCoordinate(
                        SGPdotNET.Util.Angle.FromDegrees(captureCommand.CaptureLocation.Latitude),
                        SGPdotNET.Util.Angle.FromDegrees(captureCommand.CaptureLocation.Longitude),
                        0); // Ground level


                    var maxSearchDuration = TimeSpan.FromHours(48);
                    var minOffNadirDegrees = 80.0;

                    var imagingOpportunity = await Task.Run(() =>
                        imagingCalculation.FindBestImagingOpportunity(
                            sgp4Satellite,
                            targetCoordinate,
                            receptionTime,
                            maxSearchDuration
                        )
                    );

                    // Check if the opportunity is within acceptable off-nadir angle
                    if (imagingOpportunity.OffNadirDegrees > minOffNadirDegrees)
                    {
                        throw new InvalidOperationException(
                            $"No imaging opportunity found within the off-nadir limit of {minOffNadirDegrees} degrees. " +
                            $"Best opportunity found was {imagingOpportunity.OffNadirDegrees:F2} degrees off-nadir at {imagingOpportunity.ImagingTime:yyyy-MM-dd HH:mm:ss} UTC. " +
                            $"Consider increasing MaxOffNadirDegrees or choosing a different target location.");
                    }

                    // Set the calculated execution time
                    captureCommand.ExecutionTime = imagingOpportunity.ImagingTime;
                }
            }
        }

        /// <summary>
        /// Compiles all commands to CSH format
        /// </summary>
        public static async Task<List<string>> CompileAllToCsh(this List<Command> commands)
        {
            var allCommands = new List<string>();

            foreach (var command in commands)
            {
                var commandCsh = await command.CompileToCsh();
                allCommands.AddRange(commandCsh);
            }

            return allCommands;
        }

        /// <summary>
        /// Serializes commands to JSON string
        /// </summary>
        public static string ToJson(this List<Command> commands)
        {
            var options = JsonSerializerOptionsFactory.Create();
            return JsonSerializer.Serialize(commands, options);
        }

        /// <summary>
        /// Deserializes commands from JSON string
        /// </summary>
        public static List<Command> FromJson(string json)
        {
            var options = JsonSerializerOptionsFactory.Create();

            try
            {
                var commands = JsonSerializer.Deserialize<List<Command>>(json, options);
                return commands ?? [];
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid commands JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts commands to JsonDocument for database storage
        /// </summary>
        public static JsonDocument ToJsonDocument(this List<Command> commands)
        {
            var json = commands.ToJson();
            return JsonDocument.Parse(json);
        }

        /// <summary>
        /// Deserializes commands from JsonDocument
        /// </summary>
        public static List<Command> FromJsonDocument(JsonDocument document)
        {
            var json = document.RootElement.GetRawText();
            return FromJson(json);
        }
    }
}