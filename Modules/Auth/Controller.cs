using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SatOps.Modules.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetToken([FromBody] TokenRequestDto request)
        {
            var token = await _authService.GenerateTokenAsync(request);

            if (token == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            return Ok(new TokenResponseDto { AccessToken = token });
        }
    }
}