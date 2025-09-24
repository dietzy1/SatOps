using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.Satellite;

namespace SatOps.Modules.Satellite
{
    [ApiController]
    [Route("api/v1/satellites")]
    public class SatellitesController : ControllerBase
    {
        private readonly ISatelliteService _service;

        public SatellitesController(ISatelliteService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<SatelliteDto>>> List()
        {
            var items = await _service.ListAsync();
            return Ok(items.Select(MapToDto).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SatelliteDto>> Get(int id)
        {
            var item = await _service.GetAsync(id);
            if (item == null)
            {
                return NotFound($"Satellite with ID {id} not found.");
            }
            return Ok(MapToDto(item));
        }

        private static SatelliteDto MapToDto(Satellite entity)
        {
            return new SatelliteDto
            {
                Id = entity.Id,
                Name = entity.Name,
                NoradId = entity.NoradId,
                Status = entity.Status,
                TleLine1 = entity.TleLine1,
                TleLine2 = entity.TleLine2,
                LastTleUpdate = entity.LastTleUpdate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
