using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SatOps.Modules.Satellite
{
    [ApiController]
    [Route("api/v1/satellites")]
    [Authorize]
    public class SatellitesController(ISatelliteService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves all satellites
        /// </summary>
        /// <returns>A list of all satellites in the system</returns>
        [HttpGet]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<List<SatelliteDto>>> List()
        {
            var items = await service.ListAsync();
            return Ok(items.Select(MapToDto).ToList());
        }

        /// <summary>
        /// Retrieves a specific satellite by ID
        /// </summary>
        /// <param name="id">The unique identifier of the satellite</param>
        /// <returns>The satellite details including TLE data</returns>
        [HttpGet("{id:int}")]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<SatelliteDto>> Get(int id)
        {
            var item = await service.GetAsync(id);
            if (item == null)
            {
                return NotFound($"Satellite with ID {id} not found.");
            }
            return Ok(MapToDto(item));
        }

        /// <summary>
        /// Refreshes TLE data for a satellite from external sources
        /// </summary>
        /// <param name="id">The unique identifier of the satellite</param>
        /// <returns>True if TLE data was updated, false otherwise</returns>
        [HttpPost("{id:int}/tle")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<bool>> RefreshTleData(int id)
        {
            var result = await service.RefreshTleDataAsync(id);

            if (result == null)
            {
                return NotFound($"Satellite with ID {id} not found.");
            }

            return Ok(result.Value);
        }

        private static SatelliteDto MapToDto(Satellite entity)
        {
            return new SatelliteDto
            {
                Id = entity.Id,
                Name = entity.Name,
                NoradId = entity.NoradId,
                Status = entity.Status.ToScreamCase(),
                Tle = new TleDto
                {
                    Line1 = entity.TleLine1,
                    Line2 = entity.TleLine2,
                },
                CreatedAt = entity.CreatedAt,
                LastUpdate = entity.LastUpdate,
            };
        }
    }
}
