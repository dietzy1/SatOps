using Microsoft.AspNetCore.Authentication;
using SatOps.Modules.User;
using System.Security.Claims;

namespace SatOps.Authorization
{
    public class UserPermissionsClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserService _userService;

        public UserPermissionsClaimsTransformation(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity!;

            var emailClaim = newIdentity.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
            {
                return principal;
            }

            // Fetch user permissions from the database
            var permissions = await _userService.GetUserPermissionsAsync(emailClaim);
            if (permissions == null)
            {
                return principal;
            }

            // Add all scopes as 'scope' claims.
            foreach (var scope in permissions.AllScopes)
            {
                if (!newIdentity.HasClaim("scope", scope))
                {
                    newIdentity.AddClaim(new Claim("scope", scope));
                }
            }

            // Add all roles as standard 'role' claims.
            foreach (var role in permissions.AllRoles)
            {
                if (!newIdentity.HasClaim(ClaimTypes.Role, role))
                {
                    newIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            return clone;
        }
    }
}