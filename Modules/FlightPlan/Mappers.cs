using System.Text.Json;

namespace SatOps.Modules.Schedule
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
                Status = entity.Status,
                PreviousPlanId = entity.PreviousPlanId?.ToString(),
                ApproverId = entity.ApproverId,
                ApprovalDate = entity.ApprovalDate
            };
        }
    }
}