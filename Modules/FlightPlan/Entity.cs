using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.FlightPlan.Commands;



namespace SatOps.Modules.Schedule
{
    public enum FlightPlanStatus
    {
        Draft, // Initial state
        Rejected, // Rejected by approver
        Approved, // Approved but not yet associated with an overpass
        AssignedToOverpass, // Can only be set when approved prior
        Transmitted, // Can no longer be updated
        Superseded // When a new version is created
    }
    [Table("flight_plans")]
    public class FlightPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // String backing field for EF Core
        [Column("Commands", TypeName = "jsonb")]
        public string CommandsJson { get; set; } = "{}";

        // Application-facing property with polymorphic deserialization
        [NotMapped]
        public CommandSequence Commands
        {
            get
            {
                Console.WriteLine($"=== DEBUGGING CommandsJson ===");
                Console.WriteLine($"CommandsJson value: {CommandsJson}");
                Console.WriteLine($"CommandsJson length: {CommandsJson?.Length ?? 0}");

                if (string.IsNullOrEmpty(CommandsJson) || CommandsJson == "{}")
                {
                    Console.WriteLine("CommandsJson is empty or {}");
                    return new CommandSequence();
                }

                try
                {
                    var options = GetJsonSerializerOptions();
                    Console.WriteLine($"Attempting deserialization with options...");

                    var result = JsonSerializer.Deserialize<CommandSequence>(CommandsJson, options);

                    Console.WriteLine($"Deserialization successful!");
                    Console.WriteLine($"Commands count: {result?.Commands?.Count ?? 0}");

                    if (result?.Commands != null)
                    {
                        foreach (var cmd in result.Commands)
                        {
                            Console.WriteLine($"  - Command type: {cmd?.GetType().Name ?? "null"}");
                        }
                    }

                    return result ?? new CommandSequence();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!!! Error deserializing commands !!!");
                    Console.WriteLine($"Exception: {ex.GetType().Name}");
                    Console.WriteLine($"Message: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    return new CommandSequence();
                }
            }
            set
            {
                CommandsJson = JsonSerializer.Serialize(value ?? new CommandSequence(), GetJsonSerializerOptions());
            }
        }

        /*  private static JsonSerializerOptions GetJsonSerializerOptions()
         {
             var options = new JsonSerializerOptions
             {
                 PropertyNameCaseInsensitive = true,
                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                 WriteIndented = false,
                 Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
             };
             return options;
         } */

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
                //save CommandType as the 1st property in json
                // This is critical for polymorphic deserialization to work correctly
                
            };

            return options;
        }


        // Can be null if not associated with an overpass and approved
        public DateTime? ScheduledAt { get; set; }
        public int GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public int? OverpassId { get; set; } // Link to stored overpass data
        public FlightPlanStatus Status { get; set; } = FlightPlanStatus.Draft;
        public int? PreviousPlanId { get; set; }
        public int CreatedById { get; set; }
        public int? ApproverId { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}