using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SatOps.Modules.User;
using UserEntity = SatOps.Modules.User.User;

namespace SatOps.Modules.Auth
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("station/token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetStationToken([FromBody] TokenRequestDto request)
        {
            var token = await authService.GenerateGroundStationTokenAsync(request);

            if (token == null) return Unauthorized("Invalid credentials.");

            return Ok(new TokenResponseDto { AccessToken = token });
        }
    }
}