using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.Schedule;
using SatOps.Modules.Overpass;

namespace SatOps.Modules.Schedule
{
    [ApiController]
    [Route("api/v1/flight-plans")]
    public class FlightPlansController : ControllerBase
    {
        private readonly IFlightPlanService _service;

        public FlightPlansController(IFlightPlanService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<FlightPlanDto>>> List()
        {
            var items = await _service.ListAsync();
            return Ok(items.Select(Mappers.ToDto).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FlightPlanDto>> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(Mappers.ToDto(item));
        }

        [HttpPost]
        public async Task<ActionResult<FlightPlanDto>> Create([FromBody] CreateFlightPlanDto input)
        {
            var created = await _service.CreateAsync(input);
            var dto = Mappers.ToDto(created);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<FlightPlanDto>> Update(int id, [FromBody] CreateFlightPlanDto input)
        {
            var newVersion = await _service.CreateNewVersionAsync(id, input);
            if (newVersion == null)
            {
                return BadRequest("Could not update the flight plan. It may not be in an updateable state (draft, approved awaiting overpass, or approved).");
            }
            return Ok(Mappers.ToDto(newVersion));
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> Approve(int id, [FromBody] ApproveFlightPlanDto input)
        {
            if (input.Status != "approved" && input.Status != "rejected")
            {
                return BadRequest("Invalid status provided.");
            }

            var (success, message) = await _service.ApproveOrRejectAsync(id, input.Status);
            if (!success)
            {
                return Conflict(new { detail = message });
            }

            return Ok(new { success = true, message });
        }

        [HttpPost("{id}/associate-overpass")]
        public async Task<ActionResult> AssociateOverpass(int id, [FromBody] AssociateOverpassDto input)
        {
            // Convert DTO to OverpassWindowsCalculationRequestDto
            var overpassRequest = new OverpassWindowsCalculationRequestDto
            {
                SatelliteId = input.SatelliteId,
                GroundStationId = input.GroundStationId,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                MinimumElevation = input.MinimumElevation,
                MinimumDurationSeconds = input.MinimumDurationSeconds,
                MaxResults = 1 // We'll take the first suitable overpass
            };

            var (success, message) = await _service.AssociateWithOverpassAsync(id, overpassRequest);
            if (!success)
            {
                return Conflict(new { detail = message });
            }

            return Ok(new { success = true, message });
        }
    }
}