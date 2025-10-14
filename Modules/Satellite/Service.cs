namespace SatOps.Modules.Satellite
{
    public interface ISatelliteService
    {
        Task<List<Satellite>> ListAsync();
        Task<Satellite?> GetAsync(int id);
        Task<List<Satellite>> GetActiveSatellitesAsync();
    }

    public class SatelliteService(ISatelliteRepository repository, ICelestrackClient celestrackClient) : ISatelliteService
    {
        public Task<List<Satellite>> ListAsync()
        {
            return repository.GetAllAsync();
        }

        public async Task<Satellite?> GetAsync(int id)
        {
            var satellite = await repository.GetByIdAsync(id);

            if (satellite == null) return null;

            // Update TLE data if older than 6 hours
            if (satellite.LastUpdate.AddHours(6) < DateTime.UtcNow)
            {
                var tleData = await celestrackClient.FetchTleAsync(satellite.NoradId);
                if (tleData != null)
                {
                    var lines = tleData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 3)
                    {
                        var tleLine1 = lines[1].Trim();
                        var tleLine2 = lines[2].Trim();

                        await repository.UpdateTleAsync(id, tleLine1, tleLine2);
                        satellite.TleLine1 = tleLine1;
                        satellite.TleLine2 = tleLine2;
                        satellite.LastUpdate = DateTime.UtcNow;
                    }
                }
            }

            return satellite;
        }

        public async Task<List<Satellite>> GetActiveSatellitesAsync()
        {
            var allSatellites = await repository.GetAllAsync();
            return allSatellites.Where(s => s.Status == SatelliteStatus.Active).ToList();
        }
    }
}
