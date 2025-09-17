using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using SatOps.Services.GroundStation;

namespace SatOps.Controllers
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
                Location = CreatePoint(input.Location.Longitude ?? 0, input.Location.Latitude ?? 0),
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
                existing.Location = CreatePoint(input.Location.Longitude ?? 0, input.Location.Latitude ?? 0);
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
                    Latitude = entity.Location?.Y ?? 0,
                    Longitude = entity.Location?.X ?? 0
                },
                HttpUrl = entity.HttpUrl,
                CreatedAt = entity.CreatedAt,
                IsActive = entity.IsActive
            };
        }

        private static Point CreatePoint(double longitude, double latitude)
        {
            var point = new Point(longitude, latitude)
            {
                SRID = 4326
            };
            return point;
        }
    }
}