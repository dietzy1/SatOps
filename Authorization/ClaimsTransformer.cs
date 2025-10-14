using Microsoft.AspNetCore.Authentication;
using SatOps.Modules.User;
using System.Security.Claims;

namespace SatOps.Authorization
{
    public class UserPermissionsClaimsTransformation(IUserService userService) : IClaimsTransformation
    {
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity ||
                identity.FindFirst(ClaimTypes.Email)?.Value is not { } email)
            {
                return principal;
            }

            var permissions = await userService.GetUserPermissionsAsync(email);

            var scopesToAdd = permissions.AllScopes
                .Where(scope => !principal.HasClaim("scope", scope))
                .Select(scope => new Claim("scope", scope))
                .ToList();

            var rolesToAdd = permissions.AllRoles
                .Where(role => !principal.HasClaim(ClaimTypes.Role, role))
                .Select(role => new Claim(ClaimTypes.Role, role))
                .ToList();


            if (!scopesToAdd.Any() && !rolesToAdd.Any())
            {
                return principal;
            }

            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity!;

            newIdentity.AddClaims(scopesToAdd);
            newIdentity.AddClaims(rolesToAdd);

            return clone;
        }
    }
}