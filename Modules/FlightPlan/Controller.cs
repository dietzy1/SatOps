using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.FlightPlan;
using SatOps.Modules.Overpass;

namespace SatOps.Modules.FlightPlan
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
            if (input.Status != FlightPlanStatus.Approved.ToScreamCase() && input.Status != FlightPlanStatus.Rejected.ToScreamCase())
            {
                return BadRequest("Invalid status provided can be either APPROVED or REJECTED");
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
            var (success, message) = await _service.AssociateWithOverpassAsync(id, input);
            if (!success)
            {
                return Conflict(new { detail = message });
            }

            return Ok(new { success = true, message });
        }

        [HttpGet("imaging-opportunity")]
        public async Task<ActionResult<ImagingTimingResponseDto>> GetImagingOpportunity(
            [FromQuery] ImagingTimingRequestDto request
        )
        {
            // Validate request parameters
            if (request.SatelliteId <= 0)
                return BadRequest($"Invalid satellite ID: {request.SatelliteId}");

            if (request.TargetLatitude < -90 || request.TargetLatitude > 90)
                return BadRequest($"Target latitude must be between -90 and 90 degrees: {request.TargetLatitude}");

            if (request.TargetLongitude < -180 || request.TargetLongitude > 180)
                return BadRequest($"Target longitude must be between -180 and 180 degrees: {request.TargetLongitude}");

            if (request.MaxOffNadirDegrees <= 0 || request.MaxOffNadirDegrees > 90)
                return BadRequest($"Max off-nadir angle must be between 0 and 90 degrees: {request.MaxOffNadirDegrees}");

            if (request.MaxSearchDurationHours <= 0)
                return BadRequest($"Max search duration must be a positive number of hours: {request.MaxSearchDurationHours}");

            var result = await _service.GetImagingOpportunity(request);

            return Ok(result);
        }
    }
}