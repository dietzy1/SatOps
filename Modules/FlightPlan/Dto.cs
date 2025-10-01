using System.Text.Json.Serialization;

namespace SatOps.Modules.Schedule
{
    // We need to handle the case of being associated with an overpass and not being approved in time
    // Perhabs we need to have it so an aprove action is done with an association to an overpass
    public class FlightPlanDto
    {
        public int Id { get; set; }
        public string? PreviousPlanId { get; set; }
        public int GsId { get; set; }
        public int SatId { get; set; }
        public int? OverpassId { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();
        public string Status { get; set; } = string.Empty;
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
        public int GsId { get; set; }

        public int SatId { get; set; }

        public FlightPlanBodyDto FlightPlanBody { get; set; } = new();
    }

    // DTO for the PATCH (approve/reject) request body
    public class ApproveFlightPlanDto
    {
        public string Status { get; set; } = string.Empty;
    }

    // DTO for associating with overpass using timerange
    public class AssociateOverpassDto
    {
        public int SatelliteId { get; set; }
        public int GroundStationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

}