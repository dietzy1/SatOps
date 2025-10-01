using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.Groundstation;
using SatOps.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SatOps.Modules.Auth
{
    public interface IAuthService
    {
        Task<string?> GenerateTokenAsync(TokenRequestDto request);
    }

    public class AuthService : IAuthService
    {
        private readonly IGroundStationRepository _gsRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IGroundStationRepository gsRepository, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _gsRepository = gsRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GenerateTokenAsync(TokenRequestDto request)
        {
            // Find ground station by its public ApplicationId
            // Note: We need a new method in the repository for this.
            var station = await _gsRepository.GetByApplicationIdAsync(request.ApplicationId);

            if (station == null)
            {
                _logger.LogWarning("Authentication failed: ApplicationId {AppId} not found.", request.ApplicationId);
                return null;
            }

            // Verify the provided API key against the stored hash
            if (!ApiKeyHasher.Verify(request.ApiKey, station.ApiKeyHash))
            {
                _logger.LogWarning("Authentication failed: Invalid API Key for ApplicationId {AppId}.", request.ApplicationId);
                return null;
            }

            // Credentials are valid, generate JWT
            return GenerateJwtForGroundStation(station);
        }

        private string GenerateJwtForGroundStation(GroundStation station)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, station.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("type", "GroundStation") // Custom claim to identify the token type
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpirationHours"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}