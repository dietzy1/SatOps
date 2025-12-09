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
        Superseded,
        Failed
    }

    [Table("flight_plans")]
    public class FlightPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // --- Foreign Key Properties ---
        public int? GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public int? PreviousPlanId { get; set; }
        public int CreatedById { get; set; }
        public int? ApprovedById { get; set; }

        // --- Navigation Properties ---
        [ForeignKey(nameof(GroundStationId))]
        public virtual Groundstation.GroundStation? GroundStation { get; set; }

        [ForeignKey(nameof(SatelliteId))]
        public virtual Satellite.Satellite Satellite { get; set; } = null!;

        public virtual Overpass.Entity? Overpass { get; set; }

        [ForeignKey(nameof(PreviousPlanId))]
        public virtual FlightPlan? PreviousPlan { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User.User CreatedBy { get; set; } = null!;

        [ForeignKey(nameof(ApprovedById))]
        public virtual User.User? ApprovedBy { get; set; }

        public string? FailureReason { get; set; }

        // Store commands as JSONB array in database
        [Column(TypeName = "jsonb")]
        public JsonDocument Commands { get; set; } = JsonDocument.Parse("[]");

        public FlightPlanStatus Status { get; set; } = FlightPlanStatus.Draft;
        public DateTime? ScheduledAt { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Helper methods for working with commands
        public List<Command> GetCommands()
        {
            return CommandExtensions.FromJsonDocument(Commands);
        }

        public void SetCommands(List<Command> commands)
        {
            Commands = commands.ToJsonDocument();
        }
    }
}