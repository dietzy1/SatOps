using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SatOps.Modules.User
{
    [Table("users")]
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Viewer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Additional properties for RBAC tracking
        public List<string> AdditionalScopes { get; set; } = [];
        public List<string> AdditionalRoles { get; set; } = [];

        [JsonIgnore]
        public string? PasswordHash { get; set; }
    }

    public enum UserRole
    {
        Viewer = 0,   // Lowest permission level - can only view
        Operator = 1, // Can create/modify flight plans
        Admin = 2     // Full access
    }
}

