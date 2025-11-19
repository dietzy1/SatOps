using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using SatOps.Modules.GroundStationLink;
using SatOps.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SatOps.Modules.Groundstation;
using Moq.EntityFrameworkCore;
using SatelliteEntity = SatOps.Modules.Satellite.Satellite;
using Google.Protobuf;
using SatOps.Protos;


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

        private IFormFile CreateValidContainerFile(string fileName = "test.jpg", string contentType = "image/jpeg")
        {
            // 1. Create Dummy Image Data
            var imageBytes = Encoding.UTF8.GetBytes("This represents binary image data");

            // 2. Create Protobuf Metadata
            var metadata = new Metadata
            {
                Size = imageBytes.Length,
                Width = 1024,
                Height = 768,
                Camera = "TestCam",
                Timestamp = 123456789,
                BitsPixel = 8,
                Channels = 3
            };

            // Add a custom item to verify parsing logic
            metadata.Items.Add(new MetadataItem { Key = "prediction", IntValue = 1 });

            var metaBytes = metadata.ToByteArray();

            // 3. Construct Container: [Size (4)] + [Meta] + [Image]
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(metaBytes.Length); // Writes 4 bytes, Little Endian by default
            writer.Write(metaBytes);
            writer.Write(imageBytes);

            // Reset stream position for reading
            var containerBytes = stream.ToArray();
            var readStream = new MemoryStream(containerBytes);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(readStream.Length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(readStream);

            // Setup CopyToAsync behavior
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    readStream.Position = 0;
                    readStream.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);

            return mockFile.Object;
        }

        #region ReceiveImageDataAsync Tests

        [Fact]
        public async Task ReceiveImageDataAsync_WithValidContainer_ParsesAndSavesCorrectly()
        {
            // Arrange
            var captureTime = new DateTime(2025, 10, 20, 12, 30, 00, DateTimeKind.Utc);
            var mockFile = CreateValidContainerFile();

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

            _mockDbContext.Setup(c => c.Satellites).ReturnsDbSet(new List<SatelliteEntity> { new() { Id = 1 } });
            _mockDbContext.Setup(c => c.GroundStations).ReturnsDbSet(new List<GroundStation> { new() { Id = 1 } });

            _mockDbContext.Setup(c => c.ImageData.Add(It.IsAny<ImageData>())).Callback<ImageData>(img => capturedImageData.Add(img));
            _mockObjectStorageService.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DataType>())).ReturnsAsync("some/s3/path");

            // Act
            await _sut.ReceiveImageDataAsync(dto);

            // Assert
            // 1. Verify MinIO received the stream
            _mockObjectStorageService.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), expectedFileName, mockFile.ContentType, DataType.Image), Times.Once);

            // 2. Verify DB entity
            capturedImageData.Should().ContainSingle();
            var savedEntity = capturedImageData.First();
            savedEntity.FlightPlanId.Should().Be(dto.FlightPlanId);
            savedEntity.ImageWidth.Should().Be(1024); // From our mock Protobuf
            savedEntity.ImageHeight.Should().Be(768);

            // 3. Verify Metadata JSON extraction
            savedEntity.Metadata.Should().Contain("\"prediction\":1");
        }

        [Fact]
        public async Task ReceiveImageDataAsync_WithInvalidHeader_ThrowsArgumentException()
        {
            // Arrange - Create a file that is too small to contain the header
            var smallStream = new MemoryStream(new byte[] { 0x01, 0x02 });
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.OpenReadStream()).Returns(smallStream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                   .Callback<Stream, CancellationToken>((s, t) => { smallStream.Position = 0; smallStream.CopyTo(s); })
                   .Returns(Task.CompletedTask);

            var dto = new ImageDataReceiveDto
            {
                SatelliteId = 1,
                GroundStationId = 1,
                ImageFile = mockFile.Object
            };

            _mockDbContext.Setup(c => c.Satellites).ReturnsDbSet(new List<SatelliteEntity> { new() { Id = 1 } });
            _mockDbContext.Setup(c => c.GroundStations).ReturnsDbSet(new List<GroundStation> { new() { Id = 1 } });

            // Act
            Func<Task> act = () => _sut.ReceiveImageDataAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("File too small to contain header");
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