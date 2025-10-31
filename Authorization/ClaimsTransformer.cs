using Microsoft.AspNetCore.Authentication;
using SatOps.Modules.User;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserPermissionsClaimsTransformation> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Prevent race conditions when creating users concurrently
        private static readonly SemaphoreSlim _userCreationLock = new SemaphoreSlim(1, 1);

        public UserPermissionsClaimsTransformation(
            IUserService userService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<UserPermissionsClaimsTransformation> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

                            var userInfo = await GetAuth0UserInfoAsync();

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

        /// <summary>
        /// Fetches user profile information from Auth0 UserInfo endpoint
        /// </summary>
        private async Task<Auth0UserInfo> GetAuth0UserInfoAsync()
        {
            try
            {
                // Get the access token from the Authorization header via HttpContext
                var accessToken = _httpContextAccessor.HttpContext?.Request.Headers.Authorization
                    .ToString()
                    .Replace("Bearer ", "");

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token found in Authorization header");
                    return new Auth0UserInfo();
                }

                var client = _httpClientFactory.CreateClient();
                var auth0Domain = _configuration["Auth0:Domain"];

                if (string.IsNullOrEmpty(auth0Domain))
                {
                    _logger.LogError("Auth0:Domain not configured");
                    return new Auth0UserInfo();
                }

                _logger.LogInformation("Calling Auth0 UserInfo endpoint: https://{Domain}/userinfo", auth0Domain);

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://{auth0Domain}/userinfo");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to fetch Auth0 user info: {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);
                    return new Auth0UserInfo();
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Auth0 UserInfo response: {Json}", json);

                var userInfo = JsonSerializer.Deserialize<Auth0UserInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userInfo != null)
                {
                    _logger.LogInformation("Parsed UserInfo - Email: {Email}, Name: {Name}, Nickname: {Nickname}, Sub: {Sub}",
                        userInfo.Email, userInfo.Name, userInfo.Nickname, userInfo.Sub);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize Auth0 UserInfo response");
                }

                return userInfo ?? new Auth0UserInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Auth0 user info");
                return new Auth0UserInfo();
            }
        }
    }

    /// <summary>
    /// Auth0 UserInfo response model
    /// </summary>
    public class Auth0UserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }
    }
}