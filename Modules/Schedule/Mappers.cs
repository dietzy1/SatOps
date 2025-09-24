using System.Text.Json;

namespace SatOps.Controllers.FlightPlan
{
    public static class Mappers
    {
        public static FlightPlanDto ToDto(this Services.FlightPlan.FlightPlan entity)
        {
            return new FlightPlanDto
            {
                Id = entity.Id.ToString(),
                FlightPlanBody = new FlightPlanBodyDto
                {
                    Name = entity.Name,
                    Body = JsonSerializer.Deserialize<object>(entity.Body.RootElement.GetRawText())!
                },
                ScheduledAt = entity.ScheduledAt,
                GsId = entity.GroundStationId.ToString(),
                SatName = entity.SatelliteName,
                Status = entity.Status,
                PreviousPlanId = entity.PreviousPlanId?.ToString(),
                ApproverId = entity.ApproverId,
                ApprovalDate = entity.ApprovalDate
            };
        }
    }
}