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

        [HttpGet("{id}")]
        [Authorize(Policy = "ManageUsers")]
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
                AdditionalRoles = user.AdditionalRoles,
                AdditionalScopes = user.AdditionalScopes,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
            return Ok(userDto);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManageUsers")]
        public async Task<IActionResult> UpdateUserInfo(int id, [FromBody] UpdateUserInfoDto request)
        {
            var existingUser = await userService.GetAsync(id);
            if (existingUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            existingUser.Name = request.Name;
            existingUser.Email = request.Email;

            await userService.UpdateAsync(id, existingUser);

            return NoContent();
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