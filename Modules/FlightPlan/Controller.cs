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
            // Log the items using Console.WriteLine for demonstration purposes
            Console.WriteLine($"Retrieved {items.Count} flight plans.");
            foreach (var item in items)
            {
                Console.WriteLine($"FlightPlan ID: {item.Id}, Name: {item.Name}, Status: {item.Status}");
            }

            return Ok(items.Select(Mappers.ToDto).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FlightPlanDto>> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(Mappers.ToDto(item));
        }

        /// <summary>
        /// Create a new flight plan with command sequence
        /// </summary>
        /// <param name="input">Flight plan creation data</param>
        /// <returns>Created flight plan</returns>
        /// <response code="201">Flight plan created successfully</response>
        /// <response code="400">Invalid input or validation errors</response>
        /// <response code="404">Ground station or satellite not found</response>
        [HttpPost]
        [ProducesResponseType(typeof(FlightPlanDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FlightPlanDto>> Create([FromBody] CreateFlightPlanDto input)
        {
            // Model validation happens automatically via [ApiController]
            // This includes DataAnnotations AND IValidatableObject.Validate()
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var created = await _service.CreateAsync(input);
                var dto = Mappers.ToDto(created);
                return CreatedAtAction(nameof(Get), new { id = created.Id }, dto);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
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
    }

    [Serializable]
    internal class EntityNotFoundException : Exception
    {
        public EntityNotFoundException()
        {
        }

        public EntityNotFoundException(string? message) : base(message)
        {
        }

        public EntityNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}