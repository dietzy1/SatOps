using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using SatOps.Modules.GroundStationLink;
using SatOps.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SatOps.Modules.Satellite;
using SatOps.Modules.Groundstation;
using Moq.EntityFrameworkCore;

namespace SatOps.Tests.Unit.GroundStationLink
{
    public class ImageServiceUnitTests
    {
        private readonly Mock<IObjectStorageService> _mockObjectStorageService;
        private readonly Mock<SatOpsDbContext> _mockDbContext;
        private readonly ImageService _sut;

        public ImageServiceUnitTests()
        {
            _mockObjectStorageService = new Mock<IObjectStorageService>();
            _mockDbContext = new Mock<SatOpsDbContext>(new DbContextOptions<SatOpsDbContext>());
            var mockLogger = new Mock<ILogger<ImageService>>();
            _sut = new ImageService(_mockDbContext.Object, _mockObjectStorageService.Object, mockLogger.Object);
        }

        private IFormFile CreateMockFormFile(string content = "test data", string fileName = "test.jpg", string contentType = "image/jpeg")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            return mockFile.Object;
        }

        #region ReceiveImageDataAsync Tests

        [Fact]
        public async Task ReceiveImageDataAsync_WithValidData_ConstructsCorrectFileNameAndSaves()
        {
            // Arrange
            var captureTime = new DateTime(2025, 10, 20, 12, 30, 00, DateTimeKind.Utc);
            var mockFile = CreateMockFormFile();
            var dto = new ImageDataReceiveDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                FlightPlanId = 101,
                CaptureTime = captureTime,
                ImageFile = mockFile,
            };
            var expectedFileName = $"image_1_20251020_123000_{mockFile.FileName}";
            var capturedImageData = new List<ImageData>();

            _mockDbContext.Setup(c => c.Satellites).ReturnsDbSet(new List<Satellite> { new() { Id = 1 } });
            _mockDbContext.Setup(c => c.GroundStations).ReturnsDbSet(new List<GroundStation> { new() { Id = 1 } });

            _mockDbContext.Setup(c => c.ImageData.Add(It.IsAny<ImageData>())).Callback<ImageData>(img => capturedImageData.Add(img));
            _mockObjectStorageService.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DataType>())).ReturnsAsync("some/s3/path");

            // Act
            await _sut.ReceiveImageDataAsync(dto);

            // Assert
            _mockObjectStorageService.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), expectedFileName, mockFile.ContentType, DataType.Image), Times.Once);
            capturedImageData.Should().ContainSingle();
            var savedEntity = capturedImageData.First();
            savedEntity.FlightPlanId.Should().Be(dto.FlightPlanId);
            _mockDbContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task ReceiveImageDataAsync_WithFlightPlanIdZero_SavesFlightPlanIdAsNull()
        {
            // Arrange
            var dto = new ImageDataReceiveDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                FlightPlanId = 0,
                CaptureTime = DateTime.UtcNow,
                ImageFile = CreateMockFormFile(),
            };
            var capturedImageData = new List<ImageData>();

            _mockDbContext.Setup(c => c.Satellites).ReturnsDbSet(new List<Satellite> { new() { Id = 1 } });
            _mockDbContext.Setup(c => c.GroundStations).ReturnsDbSet(new List<GroundStation> { new() { Id = 1 } });

            _mockDbContext.Setup(c => c.ImageData.Add(It.IsAny<ImageData>())).Callback<ImageData>(img => capturedImageData.Add(img));

            // Act
            await _sut.ReceiveImageDataAsync(dto);

            // Assert
            capturedImageData.Should().ContainSingle();
            capturedImageData.First().FlightPlanId.Should().BeNull();
        }

        [Fact]
        public async Task ReceiveImageDataAsync_WithInvalidSatelliteId_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ImageDataReceiveDto { SatelliteId = 999, GroundStationId = 1, ImageFile = CreateMockFormFile() };

            _mockDbContext.Setup(c => c.Satellites).ReturnsDbSet(new List<Satellite>());
            _mockDbContext.Setup(c => c.GroundStations).ReturnsDbSet(new List<GroundStation> { new() { Id = 1 } });

            // Act
            Func<Task> act = () => _sut.ReceiveImageDataAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Satellite with ID {dto.SatelliteId} does not exist");
        }

        #endregion

        #region GetImagesByFlightPlanIdAsync Tests

        [Fact]
        public async Task GetImagesByFlightPlanIdAsync_WhenImagesExist_ReturnsMappedDtosWithPresignedUrls()
        {
            // Arrange
            var flightPlanId = 101;
            var imagesInDb = new List<ImageData>
            {
                new() { Id = 1, FlightPlanId = flightPlanId, S3ObjectPath = "path/1", FileName = "file1.jpg" },
                new() { Id = 2, FlightPlanId = flightPlanId, S3ObjectPath = "path/2", FileName = "file2.jpg" },
            };

            _mockDbContext.Setup(c => c.ImageData).ReturnsDbSet(imagesInDb);
            _mockObjectStorageService.Setup(s => s.GeneratePresignedUrlAsync("path/1", 1)).ReturnsAsync("http://url/1");
            _mockObjectStorageService.Setup(s => s.GeneratePresignedUrlAsync("path/2", 1)).ReturnsAsync("http://url/2");

            // Act
            var result = await _sut.GetImagesByFlightPlanIdAsync(flightPlanId);

            // Assert
            result.Should().HaveCount(2);
            result.First(r => r.ImageId == 1).Url.Should().Be("http://url/1");
            result.First(r => r.ImageId == 2).Url.Should().Be("http://url/2");
            _mockObjectStorageService.Verify(s => s.GeneratePresignedUrlAsync(It.IsAny<string>(), 1), Times.Exactly(2));
        }

        [Fact]
        public async Task GetImagesByFlightPlanIdAsync_WhenNoImagesExist_ReturnsEmptyList()
        {
            // Arrange
            _mockDbContext.Setup(c => c.ImageData).ReturnsDbSet(new List<ImageData>());

            // Act
            var result = await _sut.GetImagesByFlightPlanIdAsync(404);

            // Assert
            result.Should().BeEmpty();
            _mockObjectStorageService.Verify(s => s.GeneratePresignedUrlAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        #endregion
    }
}