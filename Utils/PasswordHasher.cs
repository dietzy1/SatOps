namespace SatOps.Utils
{
    public static class ApiKeyHasher
    {
        public static string Hash(string apiKey)
        {
            return BCrypt.Net.BCrypt.HashPassword(apiKey, workFactor: 12);
        }

        public static bool Verify(string apiKey, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(apiKey, hash);
        }
    }
}