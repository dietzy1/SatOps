using SatOps.Data;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;
using ProtoMetadata = SatOps.Protos.Metadata;
using ProtoMetadataItem = SatOps.Protos.MetadataItem;

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

                // 1. Read the raw stream into memory (we need random access to split it)
                using var memoryStream = new MemoryStream();
                await dto.ImageFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                var rawBytes = memoryStream.ToArray();

                // 2. Parse the Header (First 4 bytes = Metadata Size, Little Endian)
                if (rawBytes.Length < 4) throw new ArgumentException("File too small to contain header");

                // Ensure we read Little Endian (Satellite uses Little Endian ARM/x86)
                int metaSize = BitConverter.ToInt32(rawBytes, 0);

                // 3. Extract and Parse Metadata
                if (rawBytes.Length < 4 + metaSize) throw new ArgumentException("File incomplete (metadata truncated)");

                // Slice the metadata bytes
                var metaByteString = ByteString.CopyFrom(rawBytes, 4, metaSize);
                var parsedMeta = ProtoMetadata.Parser.ParseFrom(metaByteString);

                // 4. Extract the actual Image Data
                int imageStartIndex = 4 + metaSize;
                int imageLength = rawBytes.Length - imageStartIndex;

                if (imageLength < 0) throw new ArgumentException("Invalid file format (metadata size exceeds file size)");

                using var imageStream = new MemoryStream(rawBytes, imageStartIndex, imageLength);

                // 5. Determine actual content type (optional logic, default to provided)
                // If the pipeline used 'jpegxl_encode', this might be image/jxl
                string contentType = dto.ImageFile.ContentType;

                // 6. Upload ONLY the image part to MinIO
                var fileName = $"image_{dto.SatelliteId}_{dto.CaptureTime:yyyyMMdd_HHmmss}_{Path.GetFileName(dto.ImageFile.FileName)}";
                var s3ObjectPath = await objectStorageService.UploadFileAsync(imageStream, fileName, contentType, DataType.Image);

                // 7. Extract AI/Custom Metadata
                // Convert the Protobuf items to a dictionary for JSON storage
                var metaDict = new Dictionary<string, object>();
                foreach (var item in parsedMeta.Items)
                {
                    switch (item.ValueCase)
                    {
                        case ProtoMetadataItem.ValueOneofCase.IntValue: metaDict[item.Key] = item.IntValue; break;
                        case ProtoMetadataItem.ValueOneofCase.FloatValue: metaDict[item.Key] = item.FloatValue; break;
                        case ProtoMetadataItem.ValueOneofCase.StringValue: metaDict[item.Key] = item.StringValue; break;
                        case ProtoMetadataItem.ValueOneofCase.BoolValue: metaDict[item.Key] = item.BoolValue; break;
                    }
                }

                // Serialize metadata to JSON string for the database
                string metadataJson = System.Text.Json.JsonSerializer.Serialize(metaDict);

                var imageData = new ImageData
                {
                    SatelliteId = dto.SatelliteId,
                    GroundStationId = dto.GroundStationId,
                    FlightPlanId = dto.FlightPlanId > 0 ? dto.FlightPlanId : null,
                    CaptureTime = dto.CaptureTime,
                    S3ObjectPath = s3ObjectPath,
                    FileName = fileName,
                    FileSize = imageLength, // Store actual image size, not blob size
                    ContentType = contentType,
                    ReceivedAt = DateTime.UtcNow,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,

                    // Fill enriched fields from Protobuf
                    ImageWidth = parsedMeta.Width,
                    ImageHeight = parsedMeta.Height,
                    Metadata = metadataJson
                };

                context.ImageData.Add(imageData);
                await context.SaveChangesAsync();
                logger.LogInformation("Stored image {ImageId} (Size: {Size}, MetaSize: {MetaSize}). Parsed {MetaCount} metadata items.",
                    imageData.Id, imageLength, metaSize, parsedMeta.Items.Count);
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