using Microsoft.AspNetCore.Authorization;

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
