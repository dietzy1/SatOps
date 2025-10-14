using System.Text.Json;

namespace SatOps.Modules.FlightPlan
{
    public static class Mappers
    {
        public static FlightPlanDto ToDto(this FlightPlan entity)
        {
            return new FlightPlanDto
            {
                Id = entity.Id,
                FlightPlanBody = new FlightPlanBodyDto
                {
                    Name = entity.Name,
                    Body = JsonSerializer.Deserialize<object>(entity.Body.RootElement.GetRawText())!
                },
                ScheduledAt = entity.ScheduledAt,
                GsId = entity.GroundStationId,
                SatId = entity.SatelliteId,
                Name = entity.Name,
                Status = entity.Status.ToScreamCase(),
                OverpassId = entity.OverpassId,
                PreviousPlanId = entity.PreviousPlanId?.ToString(),
                ApproverId = entity.ApproverId,
                ApprovalDate = entity.ApprovalDate
            };
        }
    }
    public static class FlightPlanStatusExtensions
    {
        public static string ToScreamCase(this FlightPlanStatus status) =>
            status switch
            {
                FlightPlanStatus.Draft => "DRAFT",
                FlightPlanStatus.Rejected => "REJECTED",
                FlightPlanStatus.Approved => "APPROVED",
                FlightPlanStatus.AssignedToOverpass => "ASSIGNED_TO_OVERPASS",
                FlightPlanStatus.Transmitted => "TRANSMITTED",
                FlightPlanStatus.Superseded => "SUPERSEDED",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
    }
}