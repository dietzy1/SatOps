using Microsoft.AspNetCore.Authorization;
using SatOps.Modules.User;

namespace SatOps.Authorization
{
    /// <summary>
    /// Requirement for minimum role level authorization.
    /// Implements hierarchical role checking: Admin > Operator > Viewer
    /// </summary>
    public class MinimumRoleRequirement(UserRole minimumRole) : IAuthorizationRequirement
    {
        public UserRole MinimumRole { get; } = minimumRole;
    }

    /// <summary>
    /// Handler that checks if user has at least the minimum required role.
    /// Supports hierarchical roles: Admin can access Operator and Viewer resources.
    /// </summary>
    public class MinimumRoleAuthorizationHandler : AuthorizationHandler<MinimumRoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MinimumRoleRequirement requirement)
        {
            // Get the user's role from claims
            var roleClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role);
            if (roleClaim == null)
            {
                return Task.CompletedTask;
            }

            // Parse the role
            if (!Enum.TryParse<UserRole>(roleClaim.Value, ignoreCase: true, out var userRole))
            {
                return Task.CompletedTask;
            }

            // Check if user's role meets the minimum requirement
            // Higher enum values = higher permissions (Admin=2 > Operator=1 > Viewer=0)
            if (userRole >= requirement.MinimumRole)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
