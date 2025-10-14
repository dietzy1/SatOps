using Microsoft.AspNetCore.Authorization;

namespace SatOps.Authorization
{
    public class ScopeRequirement(string requiredScope) : IAuthorizationRequirement
    {
        public string RequiredScope { get; } = requiredScope;
    }

    public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ScopeRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == "scope" && c.Value == requirement.RequiredScope))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class RoleRequirement(string requiredRole) : IAuthorizationRequirement
    {
        public string RequiredRole { get; } = requiredRole;
    }

    public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (context.User.IsInRole(requirement.RequiredRole))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
