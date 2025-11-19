using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using SatOps.Modules.GroundStationLink;
using SatOps.Data;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;
using SatOps.Modules.Groundstation;
using FlightPlanEntity = SatOps.Modules.FlightPlan.FlightPlan;
using System.Text;
using Google.Protobuf;
using SatOps.Protos;

namespace SatOps.Tests
{
    public class ImageServiceTests
    {
        private readonly Mock<IObjectStorageService> _mockObjectStorageService;
        private readonly SatOpsDbContext _dbContext;
        private readonly ImageService _imageService;

        public ImageServiceTests()
        {
            var options = new DbContextOptionsBuilder<SatOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new SatOpsDbContext(options);

            _mockObjectStorageService = new Mock<IObjectStorageService>();

            var mockImageLogger = new Mock<ILogger<ImageService>>();
            _imageService = new ImageService(_dbContext, _mockObjectStorageService.Object, mockImageLogger.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _dbContext.Satellites.Add(new SatelliteEntity { Id = 1, Name = "TestSat" });
            _dbContext.GroundStations.Add(new GroundStation { Id = 1, Name = "TestGS" });
            _dbContext.FlightPlans.Add(new FlightPlanEntity { Id = 1, Name = "TestFP" });
            _dbContext.SaveChanges();
        }

        private IFormFile CreateValidContainerFile(string content, string fileName, string contentType)
        {
            // Create Raw Image Data
            var imageBytes = Encoding.UTF8.GetBytes(content);

            // Create Protobuf Metadata
            var metadata = new Metadata
            {
                Size = imageBytes.Length,
                Width = 800,
                Height = 600,
                Channels = 3,
                Camera = "IntegrationTestCam",
                Timestamp = 12345,
                BitsPixel = 8
            };
            var metaBytes = metadata.ToByteArray();

            // Construct the Container [Size(4) + Meta + Image]
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(metaBytes.Length); // 4 bytes (Little Endian)
            writer.Write(metaBytes);
            writer.Write(imageBytes);

            var containerBytes = stream.ToArray();
            var readStream = new MemoryStream(containerBytes);

            // 4. Setup Mock
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(containerBytes.Length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(readStream);


            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((target, token) =>
                {
                    readStream.Position = 0;
                    readStream.CopyTo(target);
                })
                .Returns(Task.CompletedTask);

            return mockFile.Object;
        }

        #region ImageService Tests

        [Fact]
        public async Task ReceiveImageDataAsync_WithValidReferences_UploadsToMinioAndSavesToDb()
        {
            // Arrange
            // Use new helper to create valid binary container
            var mockFile = CreateValidContainerFile("fake_image_content", "image.jpg", "image/jpeg");

            var dto = new ImageDataReceiveDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                FlightPlanId = 1,
                CaptureTime = DateTime.UtcNow,
                ImageFile = mockFile,
                Latitude = 55.0,
                Longitude = 10.0
            };

            var expectedS3Path = "images/path/file.jpg";
            _mockObjectStorageService
                .Setup(m => m.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), DataType.Image))
                .ReturnsAsync(expectedS3Path);

            // Act
            await _imageService.ReceiveImageDataAsync(dto);

            // Assert
            // Verify Minio upload was called (and that it uploaded the extracted image, not the container)
            _mockObjectStorageService.Verify(m => m.UploadFileAsync(
                It.IsAny<Stream>(),
                It.Is<string>(s => s.StartsWith($"image_{dto.SatelliteId}") && s.EndsWith(mockFile.FileName)),
                mockFile.ContentType,
                DataType.Image), Times.Once);

            // Verify metadata was saved to the database
            var savedData = await _dbContext.ImageData.FirstOrDefaultAsync();
            savedData.Should().NotBeNull();
            savedData!.SatelliteId.Should().Be(dto.SatelliteId);
            savedData.GroundStationId.Should().Be(dto.GroundStationId);
            savedData.S3ObjectPath.Should().Be(expectedS3Path);
            savedData.Latitude.Should().Be(dto.Latitude);

            // Verify extraction from Protobuf
            savedData.ImageWidth.Should().Be(800);
            savedData.ImageHeight.Should().Be(600);
        }

        [Fact]
        public async Task ReceiveImageDataAsync_WithInvalidGroundStationId_ThrowsArgumentException()
        {
            // Arrange
            // Even for failure tests, we should provide valid file structure to ensure we hit the logic we want
            var mockFile = CreateValidContainerFile("data", "image.jpg", "image/jpeg");
            var dto = new ImageDataReceiveDto
            {
                SatelliteId = 1,
                GroundStationId = 99, // Non-existent ID
                ImageFile = mockFile
            };

            // Act
            Func<Task> act = () => _imageService.ReceiveImageDataAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Ground station with ID {dto.GroundStationId} does not exist");
        }

        #endregion
    }
}