using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SatOps.Modules.Satellite
{
    public interface ICelestrackClient
    {
        Task<string?> FetchTleAsync(int noradId);
    }

    public class CelestrackClient : ICelestrackClient
    {
        string baseUrl = "https://celestrak.org/NORAD/elements/gp.php?CATNR=";

        public async Task<string?> FetchTleAsync(int noradId)
        {
            using (var httpClient = new HttpClient())
            {
                var url = $"{baseUrl}{noradId}&FORMAT=TLE";
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
}