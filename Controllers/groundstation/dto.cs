using System;
using System.ComponentModel.DataAnnotations;

namespace SatOps.Controllers
{
    public class GroundStationDto
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Range(-90, 90)]
        public double Latitude { get; set; }
        [Range(-180, 180)]
        public double Longitude { get; set; }
        [Required]
        [Url]
        public string HttpUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class GroundStationCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Range(-90, 90)]
        public double Latitude { get; set; }
        [Range(-180, 180)]
        public double Longitude { get; set; }
        [Required]
        [Url]
        public string HttpUrl { get; set; } = string.Empty;

    }

    public class GroundStationUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Range(-90, 90)]
        public double Latitude { get; set; }
        [Range(-180, 180)]
        public double Longitude { get; set; }
        [Required]
        [Url]
        public string HttpUrl { get; set; } = string.Empty;
    }

    public class GroundStationPatchDto
    {
        public string? Name { get; set; }
        [Range(-90, 90)]
        public double? Latitude { get; set; }
        [Range(-180, 180)]
        public double? Longitude { get; set; }
        [Url]
        public string? HttpUrl { get; set; }
    }
}


