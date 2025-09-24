using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatOps.Modules.Satellite
{
    [Table("satellites")]
    public class Satellite
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        // Core Satellite Information
        [Required]
        [Range(1, 999999999, ErrorMessage = "NoradId must be positive and 1 to 9 digits long")]
        public int NoradId { get; set; }
        public SatelliteStatus Status { get; set; } = SatelliteStatus.Inactive;
        public string TleLine1 { get; set; } = string.Empty;
        public string TleLine2 { get; set; } = string.Empty;
    }

    public enum SatelliteStatus
    {
        Active = 0,
        Inactive = 1,
        Deorbited = 2,
        Unknown = 3,
        Decayed = 4,
        Launched = 5
    }
}