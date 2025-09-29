using System.Text.Json.Serialization;

namespace SatOps.Modules.Schedule
{
    // The main DTO for GET requests
    public class FlightPlanDto
    {
        public int Id { get; set; }

        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();


        public DateTime ScheduledAt { get; set; }


        public int GsId { get; set; }


        public int SatId { get; set; }


        public string Status { get; set; } = string.Empty;


        public string? PreviousPlanId { get; set; }


        public string? ApproverId { get; set; }


        public DateTime? ApprovalDate { get; set; }
    }


    public class FlightPlanBodyDto
    {
        public string Name { get; set; } = string.Empty;

        public object Body { get; set; } = new object();
    }

    // DTO for the POST request body
    public class CreateFlightPlanDto
    {

        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();

        public DateTime ScheduledAt { get; set; }

        public int GsId { get; set; }

        public int SatId { get; set; }
    }

    // DTO for the PATCH (approve/reject) request body
    public class ApproveFlightPlanDto
    {
        public string Status { get; set; } = string.Empty;
    }

}