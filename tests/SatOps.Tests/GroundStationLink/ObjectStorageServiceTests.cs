using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Minio;
using FluentAssertions;
using SatOps.Modules.GroundStationLink;
using System.Text;


namespace SatOps.Tests.GroundStationLink
{
    /// <summary>
    /// Integration tests for the ObjectStorageService.
    /// These tests require a running MinIO instance, as provided by the project's docker-compose.yaml file.
    /// They verify the actual interaction with the MinIO server.
    /// </summary>
    public class ObjectStorageServiceTests : IAsyncLifetime
    {
        private readonly IObjectStorageService _sut;
        private readonly IMinioClient _minioClient;
        private readonly string _testBucketName;

        public ObjectStorageServiceTests()
        {
            var minioConfig = new Dictionary<string, string>
            {
                {"MinIO:Endpoint", "localhost:9000"},
                {"MinIO:AccessKey", "minioadmin"},
                {"MinIO:SecretKey", "minioadmin"},
                {"MinIO:Secure", "false"},
            };

            _testBucketName = $"satops-integration-tests-{Guid.NewGuid()}";
            minioConfig["MinIO:BucketName"] = _testBucketName;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(minioConfig!)
                .Build();

            _minioClient = new MinioClient()
                .WithEndpoint(configuration["MinIO:Endpoint"])
                .WithCredentials(configuration["MinIO:AccessKey"], configuration["MinIO:SecretKey"])
                .WithSSL(bool.Parse(configuration["MinIO:Secure"]!))
                .Build();

            var mockLogger = new Mock<ILogger<ObjectStorageService>>();

            _sut = new ObjectStorageService(_minioClient, configuration, mockLogger.Object);
        }

        public Task InitializeAsync() => Task.CompletedTask;


        public async Task DisposeAsync()
        {
            // List all objects in the test bucket
            var objects = new List<string>();
            var listArgs = new Minio.DataModel.Args.ListObjectsArgs().WithBucket(_testBucketName).WithRecursive(true);

            await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs))
            {
                objects.Add(item.Key);
            }

            // Delete all objects
            if (objects.Any())
            {
                var removeObjectsArgs = new Minio.DataModel.Args.RemoveObjectsArgs().WithBucket(_testBucketName).WithObjects(objects);
                await _minioClient.RemoveObjectsAsync(removeObjectsArgs);
            }

            // Finally, delete the bucket itself
            var removeBucketArgs = new Minio.DataModel.Args.RemoveBucketArgs().WithBucket(_testBucketName);
            await _minioClient.RemoveBucketAsync(removeBucketArgs);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldCreateBucketAndUploadFileSuccessfully()
        {
            // Arrange
            var fileContent = "This is a test file for MinIO.";
            var fileName = "test-image.txt";
            var contentType = "text/plain";
            await using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            // Act
            var objectPath = await _sut.UploadFileAsync(fileStream, fileName, contentType, DataType.Image);

            // Assert
            objectPath.Should().NotBeNullOrEmpty();
            objectPath.Should().StartWith("images/");
            objectPath.Should().EndWith(fileName);

            // Verify the object actually exists in MinIO
            var statArgs = new Minio.DataModel.Args.StatObjectArgs().WithBucket(_testBucketName).WithObject(objectPath);
            var stats = await _minioClient.StatObjectAsync(statArgs);

            stats.Should().NotBeNull();
            stats.Size.Should().Be(fileStream.Length);
            stats.ContentType.Should().Be(contentType);
        }

        [Fact]
        public async Task GeneratePresignedUrlAsync_ShouldReturnValidUrl_ForExistingObject()
        {
            // Arrange
            // Upload a file so we have something to generate a URL for.
            var fileContent = "Test content.";
            var fileName = "presigned-test.txt";
            await using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var objectPath = await _sut.UploadFileAsync(fileStream, fileName, "text/plain", DataType.Image);

            // Act
            var url = await _sut.GeneratePresignedUrlAsync(objectPath, expiryHours: 1);

            // Assert
            url.Should().NotBeNullOrEmpty();
            // A valid pre-signed URL will contain the endpoint, bucket name, object path, and auth query parameters.
            url.Should().Contain(_testBucketName);
            url.Should().Contain(objectPath.Split('/').Last());
            url.Should().Contain("X-Amz-Algorithm");
            url.Should().Contain("X-Amz-Credential");
            url.Should().Contain("X-Amz-Signature");
        }

        [Fact]
        public async Task UploadFileAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
        {
            // Arrange
            await using var fileStream1 = new MemoryStream(Encoding.UTF8.GetBytes("first file"));
            await using var fileStream2 = new MemoryStream(Encoding.UTF8.GetBytes("second file"));

            // Act
            // The first call will trigger EnsureBucketExistsAsync to create the bucket.
            var path1 = await _sut.UploadFileAsync(fileStream1, "file1.txt", "text/plain", DataType.Image);

            // The second call will trigger EnsureBucketExistsAsync again, which should find the bucket and do nothing.
            var path2 = await _sut.UploadFileAsync(fileStream2, "file2.txt", "text/plain", DataType.Image);

            // Assert
            // The main assertion is that the second call did not throw an exception (e.g., "bucket already exists").
            var stat1 = await _minioClient.StatObjectAsync(new Minio.DataModel.Args.StatObjectArgs().WithBucket(_testBucketName).WithObject(path1));
            var stat2 = await _minioClient.StatObjectAsync(new Minio.DataModel.Args.StatObjectArgs().WithBucket(_testBucketName).WithObject(path2));

            stat1.Should().NotBeNull();
            stat2.Should().NotBeNull();
        }
    }
}