using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.User;
using UserEntity = SatOps.Modules.User.User;

namespace SatOps.Modules.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly IUserService userService;

        public AuthController(IAuthService authService, IUserService userService)
        {
            this.authService = authService;
            this.userService = userService;
        }
        [HttpPost("station/token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetStationToken([FromBody] TokenRequestDto request)
        {
            var token = await authService.GenerateGroundStationTokenAsync(request);

            if (token == null) return Unauthorized("Invalid credentials.");

            return Ok(new TokenResponseDto { AccessToken = token });
        }

        [HttpPost("user/register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequestDto request)
        {
            if (await userService.GetByEmailAsync(request.Email) != null)
            {
                return Conflict(new { message = "User with this email already exists." });
            }

            var user = new UserEntity
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Viewer // Default role
            };

            var createdUser = await userService.CreateAsync(user);
            return CreatedAtAction(nameof(RegisterUser), new { id = createdUser.Id }, new { createdUser.Id, createdUser.Name, createdUser.Email });
        }

        [HttpPost("user/login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> LoginUser([FromBody] UserLoginRequestDto request)
        {
            var token = await authService.GenerateUserTokenAsync(request);

            if (token == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(new TokenResponseDto { AccessToken = token });
        }
    }
}