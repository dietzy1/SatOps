namespace SatOps.Modules.Satellite
{
    public interface ISatelliteService
    {
        Task<List<Satellite>> ListAsync();
        Task<Satellite?> GetAsync(int id);
        Task<List<Satellite>> GetActiveSatellitesAsync();
    }

    public class SatelliteService : ISatelliteService
    {
        private readonly ISatelliteRepository _repository;
        private readonly ICelestrackClient _celestrackClient;

        public SatelliteService(ISatelliteRepository repository, ICelestrackClient celestrackClient)
        {
            _repository = repository;
            _celestrackClient = celestrackClient;
        }

        public Task<List<Satellite>> ListAsync()
        {
            return _repository.GetAllAsync();
        }

        public async Task<Satellite?> GetAsync(int id)
        {
            var satellite = await _repository.GetByIdAsync(id);

            if (satellite == null) return null;

            // Update TLE data if older than 6 hours
            if (satellite.LastUpdate.AddHours(6) < DateTime.UtcNow)
            {
                var tleData = await _celestrackClient.FetchTleAsync(satellite.NoradId);
                if (tleData != null)
                {
                    var lines = tleData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 3)
                    {
                        var tleDto = new TleDto
                        {
                            TleLine1 = lines[1].Trim(),
                            TleLine2 = lines[2].Trim()
                        };
                        _repository.UpdateAsync(id, tleDto);
                        satellite.TleLine1 = tleDto.TleLine1;
                        satellite.TleLine2 = tleDto.TleLine2;
                        satellite.LastUpdate = DateTime.UtcNow;
                    }
                }
            }

            return satellite;
        }

        public async Task<List<Satellite>> GetActiveSatellitesAsync()
        {
            var allSatellites = await _repository.GetAllAsync();
            return allSatellites.Where(s => s.Status == SatelliteStatus.Active).ToList();
        }
    }
}
