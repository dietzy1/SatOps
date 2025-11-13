using Xunit;
using FluentAssertions;
using SatOps.Modules.Groundstation;

namespace SatOps.Tests.Unit.Groundstation
{
    public class ApiKeyTests
    {
        [Fact]
        public void Create_And_Verify_ShouldWorkForValidKey()
        {
            // Arrange
            var rawKey = "my-secret-api-key";

            // Act
            var apiKey = ApiKey.Create(rawKey);
            var isVerified = apiKey.Verify(rawKey);

            // Assert
            apiKey.Should().NotBeNull();
            apiKey.Hash.Should().NotBe(rawKey); // Ensure it's actually hashed
            isVerified.Should().BeTrue();
        }

        [Fact]
        public void Verify_ShouldFailForInvalidKey()
        {
            // Arrange
            var correctKey = "my-secret-api-key";
            var incorrectKey = "wrong-key";
            var apiKey = ApiKey.Create(correctKey);

            // Act
            var isVerified = apiKey.Verify(incorrectKey);

            // Assert
            isVerified.Should().BeFalse();
        }

        [Fact]
        public void GenerateRawKey_ShouldReturnNonEmptyUrlSafeString()
        {
            // Act
            var rawKey = ApiKey.GenerateRawKey();

            // Assert
            rawKey.Should().NotBeNullOrEmpty();
            // Check for URL-safe characters (no '+' or '/')
            rawKey.Should().NotContain("+");
            rawKey.Should().NotContain("/");
        }
    }
}