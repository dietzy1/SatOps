using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatOps.Modules.Groundstation
{

    [Table("ground_stations")]
    public class GroundStation
    {

        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Location Location { get; set; } = null!;

        [Required]
        public string HttpUrl { get; set; } = string.Empty;

        [Required]
        public Guid ApplicationId { get; set; } = Guid.NewGuid();

        [Required]
        public string ApiKeyHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = false; // Maybe rename to IsHealthy or similar
    }

    public class Location
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double Altitude { get; set; } = 0;
    }
}