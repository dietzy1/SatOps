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

namespace SatOps.Tests
{
    public class ImageServiceTests
    {
        private readonly Mock<IObjectStorageService> _mockObjectStorageService;
        private readonly SatOpsDbContext _dbContext;
        private readonly ImageService _imageService;

        public ImageServiceTests()
        {
            // Use an in-memory database for testing to isolate from a real database
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

        /// <summary>
        /// Helper to create a mock IFormFile.
        /// </summary>
        private IFormFile CreateMockFormFile(string content, string fileName, string contentType)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            return mockFile.Object;
        }

        #region ImageService Tests

        [Fact]
        public async Task ReceiveImageDataAsync_WithValidReferences_UploadsToMinioAndSavesToDb()
        {
            // Arrange
            var mockFile = CreateMockFormFile("image data", "image.jpg", "image/jpeg");
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
            // Verify Minio upload was called correctly
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
        }

        [Fact]
        public async Task ReceiveImageDataAsync_WithInvalidGroundStationId_ThrowsArgumentException()
        {
            // Arrange
            var mockFile = CreateMockFormFile("data", "image.jpg", "image/jpeg");
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