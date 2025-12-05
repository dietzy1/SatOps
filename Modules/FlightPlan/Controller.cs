using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.GroundStationLink;

namespace SatOps.Modules.FlightPlan
{
    [ApiController]
    [Route("api/v1/flight-plans")]
    [Authorize]
    public class FlightPlansController(
        IFlightPlanService service,
        IImageService imageService,
        ILogger<FlightPlansController> logger) : ControllerBase
    {
        /// <summary>
        /// Retrieves all flight plans
        /// </summary>
        /// <returns>A list of all flight plans in the system</returns>
        [HttpGet]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<List<FlightPlanDto>>> List()
        {
            var items = await service.ListAsync();
            return Ok(items.Select(Mappers.ToDto).ToList());
        }

        /// <summary>
        /// Retrieves a specific flight plan by ID
        /// </summary>
        /// <param name="id">The unique identifier of the flight plan</param>
        /// <returns>The flight plan details</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<FlightPlanDto>> Get(int id)
        {
            var item = await service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item.ToDto());
        }

        /// <summary>
        /// Creates a new flight plan
        /// </summary>
        /// <param name="input">The flight plan creation data</param>
        /// <returns>The newly created flight plan</returns>
        [HttpPost]
        [Authorize(Policy = Authorization.Policies.RequireOperator)]
        public async Task<ActionResult<FlightPlanDto>> Create([FromBody] CreateFlightPlanDto input)
        {
            try
            {
                var created = await service.CreateAsync(input);
                var dto = created.ToDto();
                return CreatedAtAction(nameof(Get), new { id = created.Id }, dto);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid flight plan JSON: {Message}", ex.Message);
                return BadRequest(new { detail = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing flight plan by creating a new version
        /// </summary>
        /// <param name="id">The ID of the flight plan to update</param>
        /// <param name="input">The updated flight plan data</param>
        /// <returns>The updated flight plan</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = Authorization.Policies.RequireOperator)]
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
                return Ok(newVersion.ToDto());
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid flight plan update: {Message}", ex.Message);
                return BadRequest(new { detail = ex.Message });
            }
        }

        /// <summary>
        /// Approves or rejects a flight plan
        /// </summary>
        /// <param name="id">The ID of the flight plan to approve/reject</param>
        /// <param name="input">The approval status (APPROVED or REJECTED)</param>
        /// <returns>Success status and message</returns>
        // TODO: Should we be able to approve flight plans which cannot compile? Potentially we need to fix this.
        [HttpPatch("{id}")]
        [Authorize(Policy = Authorization.Policies.RequireOperator)]
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

        /// <summary>
        /// Assigns an overpass window to a flight plan
        /// </summary>
        /// <param name="id">The ID of the flight plan</param>
        /// <param name="input">The overpass assignment details</param>
        /// <returns>Success status and message</returns>
        [HttpPost("{id}/overpasses")]
        [Authorize(Policy = Authorization.Policies.RequireOperator)]
        public async Task<ActionResult> AssignOverpass(
            int id,
            [FromBody] AssignOverpassDto input)
        {
            var (success, message) = await service.AssignOverpassAsync(id, input);
            if (!success)
            {
                return Conflict(new { detail = message });
            }

            return Ok(new { success = true, message });
        }

        /// <summary>
        /// Compiles a flight plan to CSH commands
        /// </summary>
        /// <param name="id">The ID of the flight plan to compile</param>
        /// <returns>List of compiled CSH command strings</returns>
        [HttpGet("{id}/csh")]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
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

        /// <summary>
        /// Retrieves all images associated with a specific flight plan
        /// </summary>
        /// <param name="id">The ID of the flight plan</param>
        /// <returns>List of images with pre-signed URLs for download</returns>
        [HttpGet("{id}/images")]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<List<ImageResponseDto>>> GetImages(int id)
        {
            try
            {
                // Validate that the flight plan exists
                var flightPlan = await service.GetByIdAsync(id);
                if (flightPlan == null)
                {
                    logger.LogWarning("Flight plan {FlightId} not found", id);
                    return NotFound(new { detail = $"Flight plan with ID {id} not found" });
                }

                // Retrieve all images for this flight plan
                var images = await imageService.GetImagesByFlightPlanIdAsync(id);
                logger.LogInformation("Successfully retrieved {Count} images for flight plan {FlightId}", images.Count, id);
                return Ok(images);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving images for flight plan {FlightId}", id);
                return StatusCode(500, new { detail = "An error occurred while retrieving the images" });
            }
        }

        /// <summary>
        /// Calculates imaging opportunities for a target location
        /// </summary>
        /// <param name="request">The imaging timing request parameters</param>
        /// <returns>Imaging timing response with opportunity windows</returns>
        [HttpGet("imaging-opportunities")]
        [Authorize(Policy = Authorization.Policies.RequireViewer)]
        public async Task<ActionResult<ImagingTimingResponseDto>> GetImagingOpportunity([FromQuery] ImagingTimingRequestDto request)
        {
            if (request.TargetLatitude < -90 || request.TargetLatitude > 90)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Target Latitude",
                    Detail = $"Target latitude must be between -90 and 90 degrees. Received: {request.TargetLatitude}",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            if (request.TargetLongitude < -180 || request.TargetLongitude > 180)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Target Longitude",
                    Detail = $"Target longitude must be between -180 and 180 degrees. Received: {request.TargetLongitude}",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }

            try
            {
                var result = await service.GetImagingOpportunity(
                    request.SatelliteId,
                    request.TargetLatitude,
                    request.TargetLongitude,
                    request.CommandReceptionTime);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // Handle satellite not found or invalid TLE data
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (InvalidOperationException ex)
            {
                // Handle calculation errors
                logger.LogError(ex, "Error calculating imaging opportunity for satellite {SatelliteId}", request.SatelliteId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Calculation Error",
                    Detail = "An error occurred while calculating the imaging opportunity.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }
    }
}