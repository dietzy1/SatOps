using SatOps.Data;
using Microsoft.EntityFrameworkCore;

namespace SatOps.Modules.Operation
{
    // Telemetry Service
    public interface ITelemetryService
    {
        Task ReceiveTelemetryDataAsync(TelemetryDataReceiveDto dto);
    }

    public class TelemetryService : ITelemetryService
    {
        private readonly SatOpsDbContext _context;
        private readonly IMinioService _minioService;
        private readonly ILogger<TelemetryService> _logger;

        public TelemetryService(SatOpsDbContext context, IMinioService minioService, ILogger<TelemetryService> logger)
        {
            _context = context;
            _minioService = minioService;
            _logger = logger;
        }

        public async Task ReceiveTelemetryDataAsync(TelemetryDataReceiveDto dto)
        {
            try
            {
                // Validate that referenced entities exist
                await ValidateReferencesAsync(dto.SatelliteId, dto.GroundStationId, dto.FlightPlanId);

                // Store the file in MinIO
                var fileName = $"telemetry_{dto.SatelliteId}_{dto.Timestamp:yyyyMMdd_HHmmss}_{Path.GetFileName(dto.Data.FileName)}";

                using var fileStream = dto.Data.OpenReadStream();
                var s3ObjectPath = await _minioService.UploadFileAsync(fileStream, fileName, dto.Data.ContentType ?? "application/octet-stream", DataType.Telemetry);

                // Store metadata in database
                var telemetryData = new TelemetryData
                {
                    GroundStationId = dto.GroundStationId,
                    SatelliteId = dto.SatelliteId,
                    FlightPlanId = dto.FlightPlanId,
                    Timestamp = dto.Timestamp,
                    S3ObjectPath = s3ObjectPath,
                    FileName = fileName,
                    FileSize = dto.Data.Length,
                    ContentType = dto.Data.ContentType ?? "application/octet-stream",
                    ReceivedAt = DateTime.UtcNow
                };

                _context.TelemetryData.Add(telemetryData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stored telemetry data {TelemetryId} from satellite {SatelliteId}",
                    telemetryData.Id, dto.SatelliteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process telemetry data from satellite {SatelliteId}", dto.SatelliteId);
                throw;
            }
        }

        private async Task ValidateReferencesAsync(int satelliteId, int groundStationId, int flightPlanId)
        {
            // Check if satellite exists
            var satelliteExists = await _context.Satellites.AnyAsync(s => s.Id == satelliteId);
            if (!satelliteExists)
            {
                throw new ArgumentException($"Satellite with ID {satelliteId} does not exist");
            }

            // Check if ground station exists  
            var groundStationExists = await _context.GroundStations.AnyAsync(gs => gs.Id == groundStationId);
            if (!groundStationExists)
            {
                throw new ArgumentException($"Ground station with ID {groundStationId} does not exist");
            }

            // Check if flight plan exists
            var flightPlanExists = await _context.FlightPlans.AnyAsync(s => s.Id == flightPlanId);
            if (!flightPlanExists)
            {
                throw new ArgumentException($"Flight plan with ID {flightPlanId} does not exist");
            }
        }
    }

    // Image Service
    public interface IImageService
    {
        Task ReceiveImageDataAsync(ImageDataReceiveDto dto);
    }

    public class ImageService : IImageService
    {
        private readonly SatOpsDbContext _context;
        private readonly IMinioService _minioService;
        private readonly ILogger<ImageService> _logger;

        public ImageService(SatOpsDbContext context, IMinioService minioService, ILogger<ImageService> logger)
        {
            _context = context;
            _minioService = minioService;
            _logger = logger;
        }

        public async Task ReceiveImageDataAsync(ImageDataReceiveDto dto)
        {
            try
            {
                // Validate that referenced entities exist
                await ValidateReferencesAsync(dto.SatelliteId, dto.GroundStationId);

                // Store the file in MinIO
                var fileName = $"image_{dto.SatelliteId}_{dto.CaptureTime:yyyyMMdd_HHmmss}_{Path.GetFileName(dto.ImageFile.FileName)}";

                using var fileStream = dto.ImageFile.OpenReadStream();
                var s3ObjectPath = await _minioService.UploadFileAsync(fileStream, fileName, dto.ImageFile.ContentType ?? "image/jpeg", DataType.Image);

                // Extract image dimensions if possible
                int? width = null, height = null;
                try
                {
                    // Reset stream position for image processing
                    fileStream.Position = 0;
                    // You might want to add image processing library here to extract dimensions
                    // For now, we'll leave them null
                }
                catch
                {
                    // Ignore errors in image dimension extraction
                }

                // Store metadata in database
                var imageData = new ImageData
                {
                    SatelliteId = dto.SatelliteId,
                    GroundStationId = dto.GroundStationId,
                    CaptureTime = dto.CaptureTime,
                    S3ObjectPath = s3ObjectPath,
                    FileName = fileName,
                    FileSize = dto.ImageFile.Length,
                    ContentType = dto.ImageFile.ContentType ?? "image/jpeg",
                    ReceivedAt = DateTime.UtcNow,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    ImageWidth = width,
                    ImageHeight = height,
                    Metadata = dto.Metadata
                };

                _context.ImageData.Add(imageData);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stored image data {ImageId} from satellite {SatelliteId}",
                    imageData.Id, dto.SatelliteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image data from satellite {SatelliteId}", dto.SatelliteId);
                throw;
            }
        }

        private async Task ValidateReferencesAsync(int satelliteId, int groundStationId)
        {
            // Check if satellite exists
            var satelliteExists = await _context.Satellites.AnyAsync(s => s.Id == satelliteId);
            if (!satelliteExists)
            {
                throw new ArgumentException($"Satellite with ID {satelliteId} does not exist");
            }

            // Check if ground station exists  
            var groundStationExists = await _context.GroundStations.AnyAsync(gs => gs.Id == groundStationId);
            if (!groundStationExists)
            {
                throw new ArgumentException($"Ground station with ID {groundStationId} does not exist");
            }
        }
    }
}
