using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public enum FlightPlanStatus
{
    Draft, // Initial state
    Rejected, // Rejected by approver
    Approved, // Approved but not yet associated with an overpass
    AssignedToOverpass, // Can only be set when approved prior
    Transmitted, // Can no longer be updated
    Superseded // When a new version is created
}

namespace SatOps.Modules.FlightPlan
{
    [Table("flight_plans")]
    public class FlightPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public JsonDocument Body { get; set; } = null!;

        // Can be null if not associated with an overpass and approved
        public DateTime? ScheduledAt { get; set; }
        public int GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public int? OverpassId { get; set; } // Link to stored overpass data
        public FlightPlanStatus Status { get; set; } = FlightPlanStatus.Draft;
        public int? PreviousPlanId { get; set; }
        public string? ApproverId { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}