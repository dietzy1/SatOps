using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.Groundstation;
using SatOps.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserEntity = SatOps.Modules.User.User;
using SatOps.Modules.User;


namespace SatOps.Modules.Auth
{
    public interface IAuthService
    {
        Task<string?> GenerateGroundStationTokenAsync(TokenRequestDto request);
        Task<string?> GenerateUserTokenAsync(UserLoginRequestDto request);
    }

    public class AuthService(
        IGroundStationRepository gsRepository,
        IUserRepository userRepository,
        IUserService userService,
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

            if (!ApiKeyHasher.Verify(request.ApiKey, station.ApiKeyHash))
            {
                logger.LogWarning("Authentication failed: Invalid API Key for ApplicationId {AppId}.", request.ApplicationId);
                return null;
            }

            return GenerateJwtForGroundStation(station);
        }

        public async Task<string?> GenerateUserTokenAsync(UserLoginRequestDto request)
        {
            var user = await userRepository.GetByEmailAsync(request.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                logger.LogWarning("User authentication failed for email: {Email}", request.Email);
                return null;
            }

            var permissions = await userService.GetUserPermissionsAsync(request.Email);
            return GenerateJwtForUser(user, permissions);
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
                new Claim("type", "GroundStation")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings["ExpirationHours"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateJwtForUser(UserEntity user, UserPermissions permissions)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Name, user.Name),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("type", "User")
            };

            permissions.AllRoles.ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));
            permissions.AllScopes.ForEach(scope => claims.Add(new Claim("scope", scope)));

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