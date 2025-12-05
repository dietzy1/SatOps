using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.User
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UserController(IUserService userService, ICurrentUserProvider currentUserProvider) : ControllerBase
    {
        /// <summary>
        /// Retrieves all users in the system
        /// </summary>
        /// <returns>A list of all users</returns>
        [HttpGet]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await userService.ListAsync();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
            return Ok(userDtos);
        }

        /// <summary>
        /// Get the current authenticated user's profile
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userId = currentUserProvider.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User information not found in token." });
            }

            var user = await userService.GetAsync(userId.Value);
            if (user == null)
            {
                return NotFound(new { message = "User not found in database." });
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(userDto);
        }

        /// <summary>
        /// Retrieves a specific user by ID
        /// </summary>
        /// <param name="id">The unique identifier of the user</param>
        /// <returns>The user details</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await userService.GetAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            return Ok(userDto);
        }

        /// <summary>
        /// Updates a user's role
        /// </summary>
        /// <param name="id">The unique identifier of the user</param>
        /// <param name="request">The new role to assign</param>
        /// <returns>No content on success</returns>
        [HttpPut("{id}/role")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequestDto request)
        {
            var updatedUser = await userService.UpdateRoleAsync(id, request.Role);

            if (updatedUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a user from the system
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Authorization.Policies.RequireAdmin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var success = await userService.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { message = "User not found." });
            }

            return NoContent();
        }
    }
}