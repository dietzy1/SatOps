using System.ComponentModel.DataAnnotations;

namespace SatOps.Controllers
{
    public class LocationDto
    {
        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        public double? Longitude { get; set; }
    }

    public class GroundStationDto
    {
        public int Id { get; set; }

        [StringLength(20, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 20 characters")]
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Location is required")]
        public LocationDto Location { get; set; } = new LocationDto();
        [Required(ErrorMessage = "HTTP URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string HttpUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class GroundStationCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public LocationDto Location { get; set; } = new LocationDto();
        [Required]
        [Url]
        public string HttpUrl { get; set; } = string.Empty;
    }

    public class GroundStationUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public LocationDto Location { get; set; } = new LocationDto();
        [Required]
        [Url]
        public string HttpUrl { get; set; } = string.Empty;
    }

    public class GroundStationPatchDto
    {
        public string? Name { get; set; }
        public LocationDto? Location { get; set; }
        [Url]
        public string? HttpUrl { get; set; }
    }
}