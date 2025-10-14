using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SatOps.Modules.FlightPlan
{
    public enum FlightPlanStatus
    {
        Draft,
        Rejected,
        Approved,
        AssignedToOverpass,
        Transmitted,
        Superseded
    }

    [Table("flight_plans")]
    public class FlightPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public int? OverpassId { get; set; }
        public int? PreviousPlanId { get; set; }
        public int CreatedById { get; set; }
        public int? ApprovedById { get; set; }

        // Store as flat array in JSONB
        [Column(TypeName = "jsonb")]
        public JsonDocument Commands { get; set; } = JsonDocument.Parse("[]");

        public FlightPlanStatus Status { get; set; } = FlightPlanStatus.Draft;
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Helper methods for working with commands via CommandSequence
        public CommandSequence GetCommandSequence()
        {
            return CommandSequence.FromJsonDocument(Commands);
        }

        public void SetCommandSequence(CommandSequence sequence)
        {
            Commands = sequence.ToJsonDocument();
        }
    }
}