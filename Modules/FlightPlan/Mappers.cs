using System.Text.Json;
using System.Text.RegularExpressions;
using SatOps.Modules.FlightPlan;


namespace SatOps.Modules.Schedule
{
    public static class Mappers
    {
        public static FlightPlanDto ToDto(this FlightPlan entity)
        {
            return new FlightPlanDto
            {
                Id = entity.Id,
                Name = entity.Name,
                GsId = entity.GroundStationId,
                SatId = entity.SatelliteId,
                OverpassId = entity.OverpassId,
                PreviousPlanId = entity.PreviousPlanId,
                Commands = entity.Commands.Commands,
                CreatedById = entity.CreatedById,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ScheduledAt = entity.ScheduledAt,
                Status = entity.Status.ToScreamCase(),
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