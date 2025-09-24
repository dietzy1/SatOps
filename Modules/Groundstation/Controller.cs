using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.Groundstation;

namespace SatOps.Modules.Groundstation
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

        [HttpGet]
        public async Task<ActionResult<List<GroundStationDto>>> List()
        {
            var items = await _service.ListAsync();
            return Ok(items.Select(MapToDto).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GroundStationDto>> Get(int id)
        {
            var item = await _service.GetAsync(id);
            if (item == null) return NotFound();
            return Ok(MapToDto(item));
        }

        [HttpPost]
        public async Task<ActionResult<GroundStationDto>> Create([FromBody] GroundStationCreateDto input)
        {
            var entity = new GroundStation
            {
                Name = input.Name,
                Location = new Location
                {
                    Latitude = input.Location.Latitude ?? 0,
                    Longitude = input.Location.Longitude ?? 0,
                    Altitude = input.Location.Altitude ?? 0
                },
                HttpUrl = input.HttpUrl,
            };
            var created = await _service.CreateAsync(entity);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, MapToDto(created));
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<GroundStationDto>> Patch(int id, [FromBody] GroundStationPatchDto input)
        {
            var existing = await _service.GetAsync(id);
            if (existing == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(input.Name)) existing.Name = input.Name!;
            if (input.Location != null)
            {
                existing.Location = new Location
                {
                    Latitude = input.Location.Latitude ?? existing.Location.Latitude,
                    Longitude = input.Location.Longitude ?? existing.Location.Longitude,
                    Altitude = input.Location.Altitude ?? existing.Location.Altitude
                };
            }
            if (!string.IsNullOrWhiteSpace(input.HttpUrl)) existing.HttpUrl = input.HttpUrl!;

            var updated = await _service.UpdateAsync(id, existing);
            if (updated == null) return NotFound();
            return Ok(MapToDto(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
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
                    Latitude = entity.Location?.Latitude ?? 0,
                    Longitude = entity.Location?.Longitude ?? 0,
                    Altitude = entity.Location?.Altitude ?? 0
                },
                HttpUrl = entity.HttpUrl,
                CreatedAt = entity.CreatedAt,
                IsActive = entity.IsActive
            };
        }
    }
}