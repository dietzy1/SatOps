using System.Text.Json;
using System.Text.Json.Serialization;
using SatOps.Modules.FlightPlan;

namespace SatOps.Modules.Schedule
{
    public class FlightPlanDto
    {
        public int Id { get; set; }
        public int? PreviousPlanId { get; set; }
        public int GsId { get; set; }
        public int SatId { get; set; }
        public int? OverpassId { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public List<Command> Commands { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public int CreatedById { get; set; }
        public int? ApprovedById { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // DTO for the POST request body
    public class CreateFlightPlanDto
    {
        public int GsId { get; set; }
        public int SatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public JsonElement Commands { get; set; } = new();
    }

    // DTO for the PATCH (approve/reject) request body
    public class ApproveFlightPlanDto
    {
        public string Status { get; set; } = string.Empty;
    }

    // DTO for associating with overpass using timerange and optional matching criteria
    public class AssociateOverpassDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double? MaxElevation { get; set; }
        public int? DurationSeconds { get; set; }
        public DateTime? MaxElevationTime { get; set; }
    }
}