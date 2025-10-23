using System.ComponentModel.DataAnnotations;

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
        public string Status { get; set; } = string.Empty;
        public TleDto Tle { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class TleDto
    {
        [Required]
        public string Line1 { get; set; } = string.Empty;
        [Required]
        public string Line2 { get; set; } = string.Empty;
    }

    public static class SatelliteStatusExtensions
    {
        public static string ToScreamCase(this SatelliteStatus status) =>
            status switch
            {
                SatelliteStatus.Active => "ACTIVE",
                SatelliteStatus.Inactive => "INACTIVE",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
    }
}


