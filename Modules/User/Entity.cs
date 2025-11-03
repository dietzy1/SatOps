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

        /// <summary>
        /// Auth0 user ID (sub claim) - unique identifier from Auth0
        /// </summary>
        [StringLength(255)]
        public string? Auth0UserId { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Viewer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Inverse navigation properties
        [InverseProperty(nameof(FlightPlan.FlightPlan.CreatedBy))]
        public virtual ICollection<FlightPlan.FlightPlan> CreatedFlightPlans { get; set; } = new List<FlightPlan.FlightPlan>();

        [InverseProperty(nameof(FlightPlan.FlightPlan.ApprovedBy))]
        public virtual ICollection<FlightPlan.FlightPlan> ApprovedFlightPlans { get; set; } = new List<FlightPlan.FlightPlan>();


    }

    public enum UserRole
    {
        Viewer = 0,   // Lowest permission level - can only view
        Operator = 1, // Can create/modify flight plans
        Admin = 2     // Full access
    }
}

