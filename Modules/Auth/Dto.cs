using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.Auth
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

    public class UserLoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}