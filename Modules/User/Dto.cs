using System.ComponentModel.DataAnnotations;

namespace SatOps.Modules.User
{
    public class UpdateUserPermissionsRequestDto
    {
        [Required]
        public UserRole Role { get; set; }

        public List<string> AdditionalRoles { get; set; } = [];
        public List<string> AdditionalScopes { get; set; } = [];
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public List<string> AdditionalRoles { get; set; } = [];
        public List<string> AdditionalScopes { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}