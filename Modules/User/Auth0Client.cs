using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SatOps.Modules.User
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
    public class Auth0Client(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<Auth0Client> logger) : IAuth0Client
    {

        /// <summary>
        /// Fetches user profile information from Auth0 UserInfo endpoint
        /// </summary>
        public async Task<Auth0UserInfo> GetUserInfoAsync(string accessToken)
        {
            try
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    logger.LogWarning("No access token provided");
                    return new Auth0UserInfo();
                }

                var auth0Domain = configuration["Auth0:Domain"];

                if (string.IsNullOrEmpty(auth0Domain))
                {
                    logger.LogError("Auth0:Domain not configured");
                    return new Auth0UserInfo();
                }

                var client = httpClientFactory.CreateClient();

                logger.LogInformation("Calling Auth0 UserInfo endpoint: https://{Domain}/userinfo", auth0Domain);

                var request = new HttpRequestMessage(HttpMethod.Get, $"https://{auth0Domain}/userinfo");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogWarning("Failed to fetch Auth0 user info: {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);
                    return new Auth0UserInfo();
                }

                var json = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Auth0 UserInfo response: {Json}", json);

                var userInfo = JsonSerializer.Deserialize<Auth0UserInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userInfo != null)
                {
                    logger.LogInformation("Parsed UserInfo - Email: {Email}, Name: {Name}, Nickname: {Nickname}, Sub: {Sub}",
                        userInfo.Email, userInfo.Name, userInfo.Nickname, userInfo.Sub);
                }
                else
                {
                    logger.LogWarning("Failed to deserialize Auth0 UserInfo response");
                }

                return userInfo ?? new Auth0UserInfo();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching Auth0 user info");
                return new Auth0UserInfo();
            }
        }
    }


}