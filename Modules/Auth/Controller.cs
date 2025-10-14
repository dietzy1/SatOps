using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {

        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetToken([FromBody] TokenRequestDto request)
        {
            var token = await authService.GenerateTokenAsync(request);

            if (token == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new TokenResponseDto { AccessToken = token });
        }
    }
}