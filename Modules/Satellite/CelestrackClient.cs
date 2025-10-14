namespace SatOps.Modules.Satellite
{
    public interface ICelestrackClient
    {
        Task<string?> FetchTleAsync(int noradId);
    }

    public class CelestrackClient : ICelestrackClient
    {
        private const string BaseUrl = "https://celestrak.org/NORAD/elements/gp.php?CATNR=";

        public async Task<string?> FetchTleAsync(int noradId)
        {
            using var httpClient = new HttpClient();
            var url = $"{BaseUrl}{noradId}&FORMAT=TLE";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var tleData = await response.Content.ReadAsStringAsync();
                return tleData;
            }
            else
            {
                return null;
            }
        }
    }
}