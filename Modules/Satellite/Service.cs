namespace SatOps.Modules.Satellite
{
    public interface ISatelliteService
    {
        Task<List<Satellite>> ListAsync();
        Task<Satellite?> GetAsync(int id);
        Task<bool?> RefreshTleDataAsync(int satelliteId);
    }

    public class SatelliteService(
        ISatelliteRepository repository,
        ICelestrackClient celestrackClient,
        ILogger<SatelliteService> logger) : ISatelliteService
    {
        public Task<List<Satellite>> ListAsync() => repository.GetAllAsync();

        public async Task<Satellite?> GetAsync(int id)
        {
            return await repository.GetByIdAsync(id);
        }

        public async Task<bool?> RefreshTleDataAsync(int satelliteId)
        {
            var satellite = await repository.GetByIdAsync(satelliteId);
            if (satellite == null) return null;

            try
            {
                var tleData = await celestrackClient.FetchTleAsync(satellite.NoradId);
                if (string.IsNullOrWhiteSpace(tleData)) return false;

                var lines = tleData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 3) return false;

                var line1 = lines[1].Trim();
                var line2 = lines[2].Trim();

                if (satellite.TleLine1 == line1 && satellite.TleLine2 == line2)
                {
                    await repository.UpdateLastUpdateTimestampAsync(satellite.Id);
                    return true;
                }

                await repository.UpdateTleAsync(satellite.Id, line1, line2);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update TLE for Satellite {Id}", satelliteId);
                return false;
            }
        }
    }
}