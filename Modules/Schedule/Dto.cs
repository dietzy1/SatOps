using System.Text.Json.Serialization;

namespace SatOps.Controllers.FlightPlan
{
    // The main DTO for GET requests
    public class FlightPlanDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("flight_plan")]
        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();

        [JsonPropertyName("scheduled_at")]
        public DateTime ScheduledAt { get; set; }

        [JsonPropertyName("gs_id")]
        public string GsId { get; set; } = string.Empty;

        [JsonPropertyName("sat_name")]
        public string SatName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("previous_plan_id")]
        public string? PreviousPlanId { get; set; }

        [JsonPropertyName("approver_id")]
        public string? ApproverId { get; set; }

        [JsonPropertyName("approval_date")]
        public DateTime? ApprovalDate { get; set; }
    }

    public class FlightPlanBodyDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public object Body { get; set; } = new object();
    }

    // DTO for the POST request body
    public class CreateFlightPlanDto
    {
        [JsonPropertyName("flight_plan")]
        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();
        [JsonPropertyName("scheduled_at")]
        public DateTime ScheduledAt { get; set; }
        [JsonPropertyName("gs_id")]
        public string GsId { get; set; } = string.Empty;
        [JsonPropertyName("sat_name")]
        public string SatName { get; set; } = string.Empty;
    }

    // DTO for the PATCH (approve/reject) request body
    public class ApproveFlightPlanDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}