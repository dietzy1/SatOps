using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("station/token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetStationToken([FromBody] TokenRequestDto request)
        {
            var token = await authService.GenerateGroundStationTokenAsync(request);

            if (token == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new TokenResponseDto { AccessToken = token });
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