using System.Security.Cryptography;

namespace SatOps.Modules.Groundstation
{
    public class ApiKey
    {
        public string Hash { get; private set; }

        private ApiKey(string hash)
        {
            Hash = hash;
        }

        public static ApiKey Create(string rawKey)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(rawKey, workFactor: 12);
            return new ApiKey(hash);
        }

        public static ApiKey FromHash(string storedHash)
        {
            return new ApiKey(storedHash);
        }

        public bool Verify(string rawKey)
        {
            return BCrypt.Net.BCrypt.Verify(rawKey, Hash);
        }

        public static string GenerateRawKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                          .Replace('+', '-')
                          .Replace('/', '_');
        }
    }
}