using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SatOps.Modules.Schedule
{
    [Table("flight_plans")]
    public class FlightPlan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public JsonDocument Body { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }
        public Guid GroundStationId { get; set; }
        public string SatelliteName { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, approved, rejected, superseded, transmitted

        // Versioning and Auditing
        public Guid? PreviousPlanId { get; set; }
        public string? ApproverId { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}