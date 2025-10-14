using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Groundstation
{
    [ApiController]
    [Route("api/v1/ground-stations")]
    public class GroundStationsManagementController(IGroundStationService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GroundStationDto>>> List()
        {
            var items = await service.ListAsync();
            return Ok(items.Select(MapToDto).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GroundStationDto>> Get(int id)
        {
            var item = await service.GetAsync(id);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        [HttpPost]
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
                },
                HttpUrl = input.HttpUrl,
            };

            var (created, rawApiKey) = await service.CreateAsync(entity);

            var responseDto = MapToWithApiKeyDto(created, rawApiKey);

            return CreatedAtAction(nameof(Get), new { id = created.Id }, responseDto);
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<GroundStationDto>> Patch(int id, [FromBody] GroundStationPatchDto input)
        {
            var updatedEntity = await service.PatchAsync(id, input);
            if (updatedEntity == null)
            {
                return NotFound();
            }
            return Ok(MapToDto(updatedEntity));
        }

        [HttpDelete("{id:int}")]
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
                HttpUrl = entity.HttpUrl,
                CreatedAt = entity.CreatedAt,
                IsActive = entity.IsActive
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
                HttpUrl = entity.HttpUrl,
                CreatedAt = entity.CreatedAt,
                IsActive = entity.IsActive
            };
        }
    }
}