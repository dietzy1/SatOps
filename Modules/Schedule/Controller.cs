using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.Schedule;

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


        //        [Authorize(Policy = "ReadFlightPlans")]
        [HttpGet]
        public async Task<ActionResult<List<FlightPlanDto>>> List()
        {
            var items = await _service.ListAsync();
            return Ok(items.Select(Mappers.ToDto).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FlightPlanDto>> Get(Guid id)
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
        public async Task<ActionResult<FlightPlanDto>> Update(Guid id, [FromBody] CreateFlightPlanDto input)
        {
            var newVersion = await _service.CreateNewVersionAsync(id, input);
            if (newVersion == null)
            {
                return BadRequest("Could not update the flight plan. It may not be in a 'pending' state.");
            }
            return Ok(Mappers.ToDto(newVersion));
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> Approve(Guid id, [FromBody] ApproveFlightPlanDto input)
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
    }
}