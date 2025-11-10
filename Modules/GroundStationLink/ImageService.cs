using SatOps.Data;
using Microsoft.EntityFrameworkCore;

namespace SatOps.Modules.GroundStationLink
{
    public interface IImageService
    {
        Task ReceiveImageDataAsync(ImageDataReceiveDto dto);
        Task<List<ImageResponseDto>> GetImagesByFlightPlanIdAsync(int flightPlanId);
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
                    FlightPlanId = dto.FlightPlanId > 0 ? dto.FlightPlanId : null,
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

        /// <summary>
        /// Retrieves all images associated with a specific flight plan
        /// </summary>
        /// <param name="flightPlanId">The ID of the flight plan</param>
        /// <returns>List of images with pre-signed URLs</returns>
        public async Task<List<ImageResponseDto>> GetImagesByFlightPlanIdAsync(int flightPlanId)
        {
            try
            {
                var images = await context.ImageData
                    .Where(img => img.FlightPlanId == flightPlanId)
                    .OrderByDescending(img => img.CaptureTime)
                    .ToListAsync();

                var imageResponses = new List<ImageResponseDto>();

                foreach (var image in images)
                {
                    // Generate pre-signed URL (valid for 1 hour)
                    var presignedUrl = await objectStorageService.GeneratePresignedUrlAsync(image.S3ObjectPath, expiryHours: 1);
                    var expiresAt = DateTime.UtcNow.AddHours(1);

                    imageResponses.Add(new ImageResponseDto
                    {
                        ImageId = image.Id,
                        FlightPlanId = image.FlightPlanId,
                        FileName = image.FileName,
                        CaptureTime = image.CaptureTime,
                        Url = presignedUrl,
                        ExpiresAt = expiresAt,
                        ContentType = image.ContentType,
                        FileSize = image.FileSize,
                        Latitude = image.Latitude,
                        Longitude = image.Longitude
                    });
                }

                logger.LogInformation("Retrieved {Count} images for flight plan {FlightPlanId}", imageResponses.Count, flightPlanId);
                return imageResponses;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve images for flight plan {FlightPlanId}", flightPlanId);
                throw;
            }
        }
    }
}