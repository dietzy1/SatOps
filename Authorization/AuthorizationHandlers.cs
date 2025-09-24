using Microsoft.AspNetCore.Authorization;
using SatOps.Modules.User;
using System.Security.Claims;

namespace SatOps.Authorization
{
    public class ScopeRequirement : IAuthorizationRequirement
    {
        public string RequiredScope { get; }

        public ScopeRequirement(string requiredScope)
        {
            RequiredScope = requiredScope;
        }
    }

    public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
    {
        private readonly IUserService _userService;

        public ScopeAuthorizationHandler(IUserService userService)
        {
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ScopeRequirement requirement)
        {
            var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return;
            }

            var hasPermission = await _userService.HasPermissionAsync(emailClaim, requirement.RequiredScope);
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }

    public class RoleRequirement : IAuthorizationRequirement
    {
        public string RequiredRole { get; }

        public RoleRequirement(string requiredRole)
        {
            RequiredRole = requiredRole;
        }
    }

    public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly IUserService _userService;

        public RoleAuthorizationHandler(IUserService userService)
        {
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return;
            }

            var hasRole = await _userService.HasRoleAsync(emailClaim, requirement.RequiredRole);
            if (hasRole)
            {
                context.Succeed(requirement);
            }
        }
    }
}
