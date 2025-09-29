using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Groundstation
{
    [ApiController]
    [Route("api/v1/ground-stations")]
    public class GroundStationsManagementController : ControllerBase
    {
        private readonly IGroundStationService _service;

        public GroundStationsManagementController(IGroundStationService service)
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
                    // [Required] attribute guarantees these are not null
                    Latitude = input.Location.Latitude!.Value,
                    Longitude = input.Location.Longitude!.Value,
                    Altitude = input.Location.Altitude.GetValueOrDefault()
                },
                HttpUrl = input.HttpUrl,
            };
            var created = await _service.CreateAsync(entity);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, MapToDto(created));
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult<GroundStationDto>> Patch(int id, [FromBody] GroundStationPatchDto input)
        {
            var updatedEntity = await _service.PatchAsync(id, input);
            if (updatedEntity == null)
            {
                return NotFound();
            }
            return Ok(MapToDto(updatedEntity));
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