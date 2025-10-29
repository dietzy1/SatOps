using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.FlightPlan
{
    [ApiController]
    [Route("api/v1/flight-plans")]
    [Authorize]
    public class FlightPlansController(IFlightPlanService service, ILogger<FlightPlansController> logger) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = Authorization.Policies.ReadFlightPlans)]
        public async Task<ActionResult<List<FlightPlanDto>>> List()
        {
            var items = await service.ListAsync();
            return Ok(items.Select(Mappers.ToDto).ToList());
        }

        [HttpGet("{id}")]
        [Authorize(Policy = Authorization.Policies.ReadFlightPlans)]
        public async Task<ActionResult<FlightPlanDto>> Get(int id)
        {
            var item = await service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item.ToDto());
        }

        [HttpPost]
        [Authorize(Policy = Authorization.Policies.WriteFlightPlans)]
        public async Task<ActionResult<FlightPlanDto>> Create([FromBody] CreateFlightPlanDto input)
        {
            try
            {
                var created = await service.CreateAsync(input);
                var dto = Mappers.ToDto(created);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, dto);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid flight plan JSON: {Message}", ex.Message);
                return BadRequest(new { detail = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized flight plan creation attempt: {Detail}", ex.Message);
                return Forbid(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Authorization.Policies.WriteFlightPlans)]
        public async Task<ActionResult<FlightPlanDto>> Update(
            int id,
            [FromBody] CreateFlightPlanDto input)
        {
            try
            {
                var newVersion = await service.CreateNewVersionAsync(id, input);
                if (newVersion == null)
                {
                    return BadRequest(new
                    {
                        detail = "Could not update the flight plan. " +
                                "It may not be in an updateable state."
                    });
                }
                return Ok(Mappers.ToDto(newVersion));
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid flight plan update: {Message}", ex.Message);
                return BadRequest(new { detail = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized flight plan update attempt: {Detail}", ex.Message);
                return Forbid(ex.Message);
            }
        }

        // TODO: Should we be able to approve flight plans which cannot compile? Potentially we need to fix this.
        [HttpPatch("{id}")]
        [Authorize(Policy = Authorization.Policies.WriteFlightPlans)]
        public async Task<ActionResult> Approve(int id, [FromBody] ApproveFlightPlanDto input)
        {
            if (input.Status != "APPROVED" && input.Status != "REJECTED")
            {
                return BadRequest(new
                {
                    detail = "Invalid status. Must be APPROVED or REJECTED"
                });
            }

            var (success, message) = await service.ApproveOrRejectAsync(id, input.Status);
            if (!success)
            {
                return Conflict(new { detail = message });
            }

            return Ok(new { success = true, message });
        }

        // We now have a bug where we are assigning flight plans to the incorrect overpass. Its asigning it to the one before the correct one.
        // We must investigate the root cause and fix it.
        [HttpPost("{id}/overpasses")]
        [Authorize(Policy = Authorization.Policies.WriteFlightPlans)]
        public async Task<ActionResult> AssociateOverpass(
            int id,
            [FromBody] AssociateOverpassDto input)
        {
            var (success, message) = await service.AssociateWithOverpassAsync(id, input);
            if (!success)
            {
                return Conflict(new { detail = message });
            }

            return Ok(new { success = true, message });
        }

        [HttpGet("{id}/csh")]
        [Authorize(Policy = Authorization.Policies.ReadFlightPlans)]
        public async Task<ActionResult<List<string>>> CompileFlightPlan(int id)
        {
            try
            {
                var cshCommands = await service.CompileFlightPlanToCshAsync(id);
                return Ok(cshCommands);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { detail = ex.Message });
            }
        }


        [HttpGet("imaging-opportunities")]
        [Authorize(Policy = Authorization.Policies.ReadFlightPlans)]
        public async Task<ActionResult<ImagingTimingResponseDto>> GetImagingOpportunity([FromQuery] ImagingTimingRequestDto request)
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

            var result = await service.GetImagingOpportunity(request);

            return Ok(result);
        }
    }
}