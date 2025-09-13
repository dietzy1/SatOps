using System;
using System.ComponentModel.DataAnnotations;
using SatOps.Services.Satellite;

namespace SatOps.Controllers.Satellite
{
    public class SatelliteDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "NORAD ID is required")]
        public string NoradId { get; set; } = string.Empty;

        public SatelliteStatus Status { get; set; }

        public string? TleLine1 { get; set; }
        public string? TleLine2 { get; set; }
        public DateTime? LastTleUpdate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
