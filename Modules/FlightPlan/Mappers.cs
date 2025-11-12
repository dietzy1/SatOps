namespace SatOps.Modules.FlightPlan
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
                OverpassId = entity.Overpass?.Id,
                PreviousPlanId = entity.PreviousPlanId,
                CreatedById = entity.CreatedById,
                ApprovedById = entity.ApprovedById,
                Commands = entity.GetCommands(),
                ScheduledAt = entity.ScheduledAt,
                Status = entity.Status.ToScreamCase(),
                ApprovalDate = entity.ApprovalDate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
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
                FlightPlanStatus.Failed => "FAILED",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };

        public static FlightPlanStatus FromScreamCase(string status) =>
            status.ToUpperInvariant() switch
            {
                "DRAFT" => FlightPlanStatus.Draft,
                "REJECTED" => FlightPlanStatus.Rejected,
                "APPROVED" => FlightPlanStatus.Approved,
                "ASSIGNED_TO_OVERPASS" => FlightPlanStatus.AssignedToOverpass,
                "TRANSMITTED" => FlightPlanStatus.Transmitted,
                "SUPERSEDED" => FlightPlanStatus.Superseded,
                "FAILED" => FlightPlanStatus.Failed,
                _ => throw new ArgumentException($"Invalid status: {status}", nameof(status))
            };
    }
}