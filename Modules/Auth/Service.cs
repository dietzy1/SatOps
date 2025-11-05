using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.Groundstation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



namespace SatOps.Modules.Auth
{
    public interface IAuthService
    {
        Task<string?> GenerateGroundStationTokenAsync(TokenRequestDto request);
    }

    public class AuthService(
        IGroundStationRepository gsRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger) : IAuthService
    {
        public async Task<string?> GenerateGroundStationTokenAsync(TokenRequestDto request)
        {
            var station = await gsRepository.GetByApplicationIdAsync(request.ApplicationId);

            if (station == null)
            {
                logger.LogWarning("Authentication failed: ApplicationId {AppId} not found.", request.ApplicationId);
                return null;
            }

            var apiKey = ApiKey.FromHash(station.ApiKeyHash);

            if (!apiKey.Verify(request.ApiKey))
            {
                logger.LogWarning("Authentication failed: Invalid API Key for ApplicationId {AppId}.", request.ApplicationId);
                return null;
            }

            return GenerateJwtForGroundStation(station);
        }
        private string GenerateJwtForGroundStation(GroundStation station)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, station.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("type", "GroundStation"),
                // Ground stations only have access to these three operations
                new Claim("scope", Authorization.GroundStationScopes.UploadTelemetry),
                new Claim("scope", Authorization.GroundStationScopes.UploadImages),
                new Claim("scope", Authorization.GroundStationScopes.EstablishWebSocket)
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