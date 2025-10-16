using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.User
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController(IUserService userService) : ControllerBase
    {

        [HttpGet]
        [Authorize(Policy = "ManageUsers")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await userService.ListAsync();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                AdditionalRoles = u.AdditionalRoles,
                AdditionalScopes = u.AdditionalScopes,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
            return Ok(userDtos);
        }

        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> UpdateUserPermissions(int id, [FromBody] UpdateUserPermissionsRequestDto request)
        {
            var updatedUser = await userService.UpdatePermissionsAsync(id, request.Role, request.AdditionalRoles, request.AdditionalScopes);

            if (updatedUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ManageUsers")]
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