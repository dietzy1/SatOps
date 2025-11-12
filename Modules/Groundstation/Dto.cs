using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.Groundstation
{
    public class TokenRequestDto
    {
        [Required]
        public Guid ApplicationId { get; set; }

        [Required]
        public string ApiKey { get; set; } = string.Empty;
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
    }
    public class LocationDto
    {
        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
        public double? Longitude { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Altitude must be a positive number")]
        public double? Altitude { get; set; } = 0;
    }

    public class GroundStationDto
    {
        public int Id { get; set; }

        [StringLength(20, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 20 characters")]
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Location is required")]
        public LocationDto Location { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        public bool Connected { get; set; }
    }

    public class GroundStationCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public LocationDto Location { get; set; } = new();
    }

    public class GroundStationWithApiKeyDto
    {
        [Required]
        public int Id { get; set; }

        [StringLength(20, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 20 characters")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid ApplicationId { get; set; }

        [Required]
        public string RawApiKey { get; set; } = string.Empty; // One-time secret

        [Required(ErrorMessage = "Location is required")]
        public LocationDto Location { get; set; } = new();

        [Required]
        public DateTime CreatedAt { get; set; }
    }

    public class GroundStationPatchDto
    {
        public string? Name { get; set; }
        public LocationDto? Location { get; set; }
    }

    public class GroundStationHealthDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Connected { get; set; }
        public DateTime CheckedAt { get; set; }
    }
}