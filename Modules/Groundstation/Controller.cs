using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SatOps.Modules.GroundStationLink;

namespace SatOps.Modules.Groundstation
{
    [ApiController]
    [Route("api/v1/ground-stations")]
    [Authorize]
    public class GroundStationsManagementController(
        IGroundStationService service,
        IWebSocketService gatewayService) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<List<GroundStationDto>>> List()
        {
            var items = await service.ListAsync();
            return Ok(items.Select(MapToDto).ToList());
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<GroundStationDto>> Get(int id)
        {
            var item = await service.GetAsync(id);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        [HttpGet("{id:int}/health")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<GroundStationHealthDto>> GetHealth(int id)
        {
            var station = await service.GetAsync(id);
            if (station == null)
            {
                return NotFound($"Ground station with ID {id} not found");
            }

            var isConnected = gatewayService.IsGroundStationConnected(id);

            var healthDto = new GroundStationHealthDto
            {
                Id = station.Id,
                Name = station.Name,
                Connected = isConnected,
                CheckedAt = DateTime.UtcNow
            };
            return Ok(healthDto);
        }

        [HttpPost]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<GroundStationWithApiKeyDto>> Create([FromBody] GroundStationCreateDto input)
        {
            var entity = new GroundStation
            {
                Name = input.Name,
                Location = new Location
                {
                    Latitude = input.Location.Latitude!.Value,
                    Longitude = input.Location.Longitude!.Value,
                    Altitude = input.Location.Altitude.GetValueOrDefault()
                }
            };
            var (created, rawApiKey) = await service.CreateAsync(entity);
            var responseDto = MapToWithApiKeyDto(created, rawApiKey);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, responseDto);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<GroundStationDto>> Patch(int id, [FromBody] GroundStationPatchDto input)
        {
            var updatedEntity = await service.PatchAsync(id, input);
            if (updatedEntity == null) return NotFound();
            return Ok(MapToDto(updatedEntity));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        private static GroundStationDto MapToDto(GroundStation entity)
        {
            return new GroundStationDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Location = new LocationDto
                {
                    Latitude = entity.Location.Latitude,
                    Longitude = entity.Location.Longitude,
                    Altitude = entity.Location.Altitude
                },
                CreatedAt = entity.CreatedAt,
                Connected = entity.Connected
            };
        }

        private static GroundStationWithApiKeyDto MapToWithApiKeyDto(GroundStation entity, string rawApiKey)
        {
            return new GroundStationWithApiKeyDto
            {
                Id = entity.Id,
                Name = entity.Name,
                ApplicationId = entity.ApplicationId,
                RawApiKey = rawApiKey,
                Location = new LocationDto
                {
                    Latitude = entity.Location.Latitude,
                    Longitude = entity.Location.Longitude,
                    Altitude = entity.Location.Altitude
                },
                CreatedAt = entity.CreatedAt
            };
        }
    }
}