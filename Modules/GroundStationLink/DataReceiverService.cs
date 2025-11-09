using SatOps.Data;
using Microsoft.EntityFrameworkCore;

namespace SatOps.Modules.GroundStationLink
{
    public interface ITelemetryService
    {
        Task ReceiveTelemetryDataAsync(TelemetryDataReceiveDto dto);
    }

    public class TelemetryService(SatOpsDbContext context, ILogger<TelemetryService> logger) : ITelemetryService
    {
        public async Task ReceiveTelemetryDataAsync(TelemetryDataReceiveDto dto)
        {
            try
            {
                logger.LogInformation("Receiving telemetry data from satellite {SatelliteId} via ground station {GroundStationId}", dto.SatelliteId, dto.GroundStationId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process telemetry data from satellite {SatelliteId}", dto.SatelliteId);
                throw;
            }
        }

        private async Task ValidateReferencesAsync(int satelliteId, int groundStationId, int flightPlanId)
        {
            if (!await context.Satellites.AnyAsync(s => s.Id == satelliteId)) throw new ArgumentException($"Satellite with ID {satelliteId} does not exist");
            if (!await context.GroundStations.AnyAsync(gs => gs.Id == groundStationId)) throw new ArgumentException($"Ground station with ID {groundStationId} does not exist");
            if (!await context.FlightPlans.AnyAsync(s => s.Id == flightPlanId)) throw new ArgumentException($"Flight plan with ID {flightPlanId} does not exist");
        }
    }

    // Image Service
    public interface IImageService
    {
        Task ReceiveImageDataAsync(ImageDataReceiveDto dto);
    }

    public class ImageService(SatOpsDbContext context, IObjectStorageService objectStorageService, ILogger<ImageService> logger) : IImageService
    {
        public async Task ReceiveImageDataAsync(ImageDataReceiveDto dto)
        {
            try
            {
                await ValidateReferencesAsync(dto.SatelliteId, dto.GroundStationId);
                var fileName = $"image_{dto.SatelliteId}_{dto.CaptureTime:yyyyMMdd_HHmmss}_{Path.GetFileName(dto.ImageFile.FileName)}";
                await using var fileStream = dto.ImageFile.OpenReadStream();
                var s3ObjectPath = await objectStorageService.UploadFileAsync(fileStream, fileName, dto.ImageFile.ContentType ?? "image/jpeg", DataType.Image);
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
                };
                context.ImageData.Add(imageData);
                await context.SaveChangesAsync();
                logger.LogInformation("Stored image data {ImageId} from satellite {SatelliteId}", imageData.Id, dto.SatelliteId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process image data from satellite {SatelliteId}", dto.SatelliteId);
                throw;
            }
        }

        private async Task ValidateReferencesAsync(int satelliteId, int groundStationId)
        {
            if (!await context.Satellites.AnyAsync(s => s.Id == satelliteId)) throw new ArgumentException($"Satellite with ID {satelliteId} does not exist");
            if (!await context.GroundStations.AnyAsync(gs => gs.Id == groundStationId)) throw new ArgumentException($"Ground station with ID {groundStationId} does not exist");
        }
    }
}