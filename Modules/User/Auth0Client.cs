using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SatOps.Authorization
{
    /// <summary>
    /// Client for Auth0 API operations
    /// </summary>
    public interface IAuth0Client
    {
        /// <summary>
        /// Fetches user profile information from Auth0 UserInfo endpoint
        /// </summary>
        Task<Auth0UserInfo> GetUserInfoAsync(string accessToken);
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

    /// <summary>
    /// Implementation of Auth0 API client
    /// </summary>
    public class Auth0Client : IAuth0Client
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Auth0Client> _logger;

        public Auth0Client(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<Auth0Client> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Fetches user profile information from Auth0 UserInfo endpoint
        /// </summary>
        public async Task<Auth0UserInfo> GetUserInfoAsync(string accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("No access token provided");
                    return new Auth0UserInfo();
                }

                var auth0Domain = _configuration["Auth0:Domain"];

                if (string.IsNullOrEmpty(auth0Domain))
                {
                    _logger.LogError("Auth0:Domain not configured");
                    return new Auth0UserInfo();
                }

                var client = _httpClientFactory.CreateClient();

                _logger.LogInformation("Calling Auth0 UserInfo endpoint: https://{Domain}/userinfo", auth0Domain);

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://{auth0Domain}/userinfo");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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


}