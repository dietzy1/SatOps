using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using SatOps.Modules.User;
using System.Security.Claims;

namespace SatOps.Authorization
{
    public class UserPermissionsClaimsTransformation(
        IUserService userService,
        ILogger<UserPermissionsClaimsTransformation> logger,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache) : IClaimsTransformation
    {
        private static readonly SemaphoreSlim UserCreationLock = new(1, 1);
        private record CachedUserData(int UserId, UserRole Role);

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            {
                return principal;
            }

            // Skip claims transformation for ground station tokens
            // Ground stations authenticate via their own JWT scheme and don't need user records
            var typeClaim = identity.FindFirst("type");
            if (typeClaim?.Value == "GroundStation")
            {
                logger.LogDebug("Skipping user claims transformation for ground station token");
                return principal;
            }

            var auth0UserId = identity.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(auth0UserId))
            {
                logger.LogWarning("No Auth0 user ID (sub claim) found in token");
                return principal;
            }

            var cacheKey = $"user_permissions_{auth0UserId}";

            if (cache.TryGetValue<CachedUserData>(cacheKey, out var cachedUser))
            {
                logger.LogDebug("Cache hit for user {Auth0UserId}", auth0UserId);
                return AddClaimsFromUserData(principal, cachedUser!);
            }

            logger.LogDebug("Cache miss for user {Auth0UserId}, fetching from DB.", auth0UserId);

            try
            {
                var existingUser = await userService.GetByAuth0UserIdAsync(auth0UserId);

                if (existingUser == null)
                {
                    await UserCreationLock.WaitAsync();
                    try
                    {
                        existingUser = await userService.GetByAuth0UserIdAsync(auth0UserId);
                        if (existingUser == null)
                        {
                            logger.LogInformation("New user {Auth0UserId} detected, creating user", auth0UserId);
                            var accessToken = httpContextAccessor.HttpContext?.Request.Headers.Authorization
                                .ToString()
                                .Replace("Bearer ", "") ?? string.Empty;
                            existingUser = await userService.GetOrCreateUserFromAuth0Async(
                                auth0UserId,
                                accessToken
                            );
                        }
                    }
                    finally
                    {
                        UserCreationLock.Release();
                    }
                }

                var userDataToCache = new CachedUserData(existingUser.Id, existingUser.Role);
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                cache.Set(cacheKey, userDataToCache, cacheOptions);

                return AddClaimsFromUserData(principal, userDataToCache);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error transforming claims for user {Auth0UserId}", auth0UserId);
                return principal;
            }
        }

        private ClaimsPrincipal AddClaimsFromUserData(ClaimsPrincipal principal, CachedUserData userData)
        {
            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity!;

            var roleValue = userData.Role.ToString();
            if (!principal.HasClaim(ClaimTypes.Role, roleValue))
            {
                newIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
            }

            var userIdValue = userData.UserId.ToString();
            if (!principal.HasClaim("user_id", userIdValue))
            {
                newIdentity.AddClaim(new Claim("user_id", userIdValue));
            }

            return clone;
        }
    }
}