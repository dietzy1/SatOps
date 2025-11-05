using SatOps.Modules.Gateway;

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
    }

    public class GroundStationService(IGroundStationRepository repository, IGroundStationGatewayService gatewayService) : IGroundStationService
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
            if (existing == null)
            {
                return null;
            }

            bool hasChanges = false;

            if (patchDto.Name != null && existing.Name != patchDto.Name)
            {
                existing.Name = patchDto.Name;
                hasChanges = true;
            }

            if (patchDto.Location != null)
            {
                // Only update if there are actual changes
                var loc = existing.Location;
                var newLat = patchDto.Location.Latitude ?? loc.Latitude;
                var newLon = patchDto.Location.Longitude ?? loc.Longitude;
                var newAlt = patchDto.Location.Altitude ?? loc.Altitude;

                if (IsDifferent(loc.Latitude, newLat) ||
                    IsDifferent(loc.Longitude, newLon) ||
                    IsDifferent(loc.Altitude, newAlt))
                {
                    existing.Location = new Location { Latitude = newLat, Longitude = newLon, Altitude = newAlt };
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                return existing;
            }

            return await repository.UpdateAsync(existing);
        }

        public Task<bool> DeleteAsync(int id) => repository.DeleteAsync(id);

        public async Task<List<GroundStation>> GetConnectedStationsAsync()
        {
            var allStations = await repository.GetAllAsync();
            // Filter stations that are connected via WebSocket
            return allStations.Where(s => gatewayService.IsGroundStationConnected(s.Id)).ToList();
        }

        private static bool IsDifferent(double a, double b) => Math.Abs(a - b) > Epsilon;
    }
}

