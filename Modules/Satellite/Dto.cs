using System.ComponentModel.DataAnnotations;
using SatOps.Modules.Satellite;

namespace SatOps.Modules.Satellite
{
    public class SatelliteDto
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;
        [Required]
        [Range(1, 999999999, ErrorMessage = "NoradId must be positive and 1 to 9 digits long")]
        public int NoradId { get; set; }
        public SatelliteStatus Status { get; set; }
        public TleDto TleDto { get; set; } = new TleDto();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class TleDto
    {
        [Required]
        public string TleLine1 { get; set; } = string.Empty;
        [Required]
        public string TleLine2 { get; set; } = string.Empty;
    }
}


