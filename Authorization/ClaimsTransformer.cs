using Microsoft.AspNetCore.Authentication;
using SatOps.Modules.User;
using System.Security.Claims;

namespace SatOps.Authorization
{
    /// <summary>
    /// Transforms Auth0 JWT claims by loading user permissions from the database.
    /// Auto-creates users with Viewer role if they don't exist.
    /// Fetches user profile from Auth0 UserInfo endpoint for new users.
    /// </summary>
    public class UserPermissionsClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserService _userService;
        private readonly IAuth0Client _auth0Client;
        private readonly ILogger<UserPermissionsClaimsTransformation> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Prevent race conditions when creating users concurrently
        private static readonly SemaphoreSlim _userCreationLock = new SemaphoreSlim(1, 1);

        public UserPermissionsClaimsTransformation(
            IUserService userService,
            IAuth0Client auth0Client,
            ILogger<UserPermissionsClaimsTransformation> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _auth0Client = auth0Client;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            {
                return principal;
            }

            var auth0UserId = identity.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(auth0UserId))
            {
                _logger.LogWarning("No Auth0 user ID (sub claim) found in token");
                return principal;
            }

            try
            {
                var existingUser = await _userService.GetByAuth0UserIdAsync(auth0UserId);

                if (existingUser == null)
                {
                    // Use semaphore to prevent race conditions when multiple requests come in simultaneously
                    await _userCreationLock.WaitAsync();
                    try
                    {
                        // Double-check after acquiring lock
                        existingUser = await _userService.GetByAuth0UserIdAsync(auth0UserId);

                        if (existingUser == null)
                        {
                            // User doesn't exist - fetch profile from Auth0 UserInfo endpoint
                            _logger.LogInformation("New user {Auth0UserId} detected, fetching profile from Auth0", auth0UserId);

                            var accessToken = _httpContextAccessor.HttpContext?.Request.Headers.Authorization
                                .ToString()
                                .Replace("Bearer ", "");

                            var userInfo = await _auth0Client.GetUserInfoAsync(accessToken ?? string.Empty);

                            var email = userInfo.Email ?? $"{auth0UserId}@unknown.com";
                            var name = userInfo.Name ?? userInfo.Nickname ?? "Unknown User";

                            existingUser = await _userService.GetOrCreateUserFromAuth0Async(
                                auth0UserId,
                                email,
                                name
                            );

                            _logger.LogInformation("Created new user {UserId} for Auth0 user {Auth0UserId}",
                                existingUser.Id, auth0UserId);
                        }
                    }
                    finally
                    {
                        _userCreationLock.Release();
                    }
                }

                // Clone principal and add claims
                var clone = principal.Clone();
                var newIdentity = (ClaimsIdentity)clone.Identity!;

                // Add role from database to claims
                var roleValue = existingUser.Role.ToString();
                if (!principal.HasClaim(ClaimTypes.Role, roleValue))
                {
                    newIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                }

                // Add internal user ID for easy access
                if (!principal.HasClaim("user_id", existingUser.Id.ToString()))
                {
                    newIdentity.AddClaim(new Claim("user_id", existingUser.Id.ToString()));
                }

                _logger.LogDebug("Added role {Role} for user {Auth0UserId}", roleValue, auth0UserId);

                return clone;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming claims for user {Auth0UserId}", auth0UserId);
                return principal;
            }
        }
    }
}