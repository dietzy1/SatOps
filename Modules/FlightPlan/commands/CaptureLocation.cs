using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SatOps.Modules.FlightPlan.Commands
{
    /// <summary>
    /// Represents geographic coordinates for image capture location
    /// </summary>
    public class CaptureLocation
    {
        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
