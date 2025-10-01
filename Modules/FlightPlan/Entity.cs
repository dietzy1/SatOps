using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SatOps.Modules.Schedule
{
    [Table("flight_plans")]
    public class FlightPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public JsonDocument Body { get; set; } = null!;

        public DateTime ScheduledAt { get; set; }
        public int GroundStationId { get; set; }
        public int SatelliteId { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, rejected, superseded, transmitted

        // Versioning and Auditing
        public int? PreviousPlanId { get; set; }
        public string? ApproverId { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}