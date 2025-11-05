using Microsoft.IdentityModel.Tokens;
using SatOps.Modules.GroundStationLink;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SatOps.Modules.Groundstation
{
    public interface IGroundStationService
    {
        Task<List<GroundStation>> ListAsync();
        Task<GroundStation?> GetAsync(int id);
        Task<(GroundStation createdStation, string rawApiKey)> CreateAsync(GroundStation entity);
        Task<GroundStation?> PatchAsync(int id, GroundStationPatchDto patchDto);
        Task<bool> DeleteAsync(int id);
        Task<List<GroundStation>> GetConnectedStationsAsync();
        Task<string?> GenerateGroundStationTokenAsync(TokenRequestDto request);
    }

    public class GroundStationService(
        IGroundStationRepository repository,
        IWebSocketService gatewayService,
        IConfiguration configuration,
        ILogger<GroundStationService> logger
    ) : IGroundStationService
    {
        private const double Epsilon = 1e-6;

        public async Task<List<GroundStation>> ListAsync()
        {
            var stations = await repository.GetAllAsync();
            foreach (var station in stations)
            {
                station.Connected = gatewayService.IsGroundStationConnected(station.Id);
            }
            return stations;
        }

        public async Task<GroundStation?> GetAsync(int id)
        {
            var connected = gatewayService.IsGroundStationConnected(id);
            var gs = await repository.GetByIdAsync(id);
            if (gs != null)
            {
                gs.Connected = connected;
            }
            return gs;
        }

        public async Task<(GroundStation createdStation, string rawApiKey)> CreateAsync(GroundStation entity)
        {
            var rawApiKey = ApiKey.GenerateRawKey();
            var hashedKey = ApiKey.Create(rawApiKey);
            entity.ApiKeyHash = hashedKey.Hash;
            entity.ApplicationId = Guid.NewGuid();
            var createdEntity = await repository.AddAsync(entity);
            return (createdEntity, rawApiKey);
        }

        public async Task<GroundStation?> PatchAsync(int id, GroundStationPatchDto patchDto)
        {
            var existing = await repository.GetByIdTrackedAsync(id);
            if (existing == null) return null;

            bool hasChanges = false;
            if (patchDto.Name != null && existing.Name != patchDto.Name)
            {
                existing.Name = patchDto.Name;
                hasChanges = true;
            }
            if (patchDto.Location != null)
            {
                var loc = existing.Location;
                var newLat = patchDto.Location.Latitude ?? loc.Latitude;
                var newLon = patchDto.Location.Longitude ?? loc.Longitude;
                var newAlt = patchDto.Location.Altitude ?? loc.Altitude;
                if (IsDifferent(loc.Latitude, newLat) || IsDifferent(loc.Longitude, newLon) || IsDifferent(loc.Altitude, newAlt))
                {
                    existing.Location = new Location { Latitude = newLat, Longitude = newLon, Altitude = newAlt };
                    hasChanges = true;
                }
            }
            if (!hasChanges) return existing;
            return await repository.UpdateAsync(existing);
        }

        public Task<bool> DeleteAsync(int id) => repository.DeleteAsync(id);

        public async Task<List<GroundStation>> GetConnectedStationsAsync()
        {
            var allStations = await repository.GetAllAsync();
            return allStations.Where(s => gatewayService.IsGroundStationConnected(s.Id)).ToList();
        }


        public async Task<string?> GenerateGroundStationTokenAsync(TokenRequestDto request)
        {
            var station = await repository.GetByApplicationIdAsync(request.ApplicationId);
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

        private static bool IsDifferent(double a, double b) => Math.Abs(a - b) > Epsilon;
    }
}