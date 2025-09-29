using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Groundstation.Health
{
    [ApiController]
    [Route("api/v1/ground-stations")]
    public class GroundStationsController : ControllerBase
    {
        private readonly IGroundStationService _service;

        public GroundStationsController(IGroundStationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get the health status of a specific ground station
        /// </summary>
        /// <param name="id">The ground station ID</param>
        /// <returns>Health status information</returns>
        [HttpGet("{id:int}/health")]
        public async Task<ActionResult<GroundStationHealthDto>> GetHealth(int id)
        {
            var (groundStation, isHealthy) = await _service.GetRealTimeHealthStatusAsync(id);
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
