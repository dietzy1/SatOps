using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatOps.Services.Satellite
{
    [Table("satellites")]
    public class Satellite
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Core Satellite Information
        [Required]
        public string NoradId { get; set; } = string.Empty;

        public SatelliteStatus Status { get; set; } = SatelliteStatus.Inactive;

        public string? TleLine1 { get; set; }
        public string? TleLine2 { get; set; }

        public DateTime? LastTleUpdate { get; set; }
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