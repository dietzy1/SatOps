using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Groundstation.Health
{
    [ApiController]
    [Route("api/v1/ground-stations")]
    public class GroundStationsHealthController(IGroundStationService service) : ControllerBase
    {

        /// <summary>
        /// Get the health status of a specific ground station
        /// </summary>
        /// <param name="id">The ground station ID</param>
        /// <returns>Health status information</returns>
        [HttpGet("{id:int}/health")]
        public async Task<ActionResult<GroundStationHealthDto>> GetHealth(int id)
        {
            var (groundStation, isHealthy) = await service.GetRealTimeHealthStatusAsync(id);
            if (groundStation == null)
            {
                return NotFound($"Ground station with ID {id} not found");
            }

            var healthDto = new GroundStationHealthDto
            {
                Id = groundStation.Id,
                Name = groundStation.Name,
                IsActive = isHealthy,
                LastUpdated = DateTime.UtcNow
            };

            return Ok(healthDto);
        }
    }
}
