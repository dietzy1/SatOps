using Minio;
using Minio.DataModel.Args;

namespace SatOps.Modules.GroundStationLink
{
    public enum DataType
    {
        Image
    }

    public interface IObjectStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, DataType dataType);
        Task<Stream> GetFileAsync(string objectPath);
        Task DeleteFileAsync(string objectPath);
        Task<string> GeneratePresignedUrlAsync(string objectPath, int expiryHours = 1);
    }

    public class ObjectStorageService(IMinioClient minioClient, IConfiguration configuration, ILogger<ObjectStorageService> logger) : IObjectStorageService
    {
        private readonly string _bucketName = configuration.GetValue<string>("MinIO:BucketName") ?? "satops-data";

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, DataType dataType)
        {
            try
            {
                await EnsureBucketExistsAsync();
                var prefix = dataType switch
                {
                    DataType.Image => "images",
                    _ => "data"
                };
                var objectPath = $"{prefix}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{fileName}";
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);
                await minioClient.PutObjectAsync(putObjectArgs);
                logger.LogInformation("Successfully uploaded {DataType} file {FileName} to {ObjectPath}", dataType, fileName, objectPath);
                return objectPath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload {DataType} file {FileName} to MinIO", dataType, fileName);
                throw;
            }
        }

        public async Task<Stream> GetFileAsync(string objectPath)
        {
            try
            {
                var memoryStream = new MemoryStream();
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream));
                await minioClient.GetObjectAsync(getObjectArgs);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get file {ObjectPath} from MinIO", objectPath);
                throw;
            }
        }

        public async Task DeleteFileAsync(string objectPath)
        {
            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath);
                await minioClient.RemoveObjectAsync(removeObjectArgs);
                logger.LogInformation("Successfully deleted file {ObjectPath} from MinIO", objectPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file {ObjectPath} from MinIO", objectPath);
                throw;
            }
        }

        public async Task<string> GeneratePresignedUrlAsync(string objectPath, int expiryHours = 1)
        {
            try
            {
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithExpiry(expiryHours * 3600); // Convert hours to seconds

                var url = await minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
                logger.LogInformation("Generated pre-signed URL for {ObjectPath} valid for {ExpiryHours} hour(s)", objectPath, expiryHours);
                return url;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate pre-signed URL for {ObjectPath}", objectPath);
                throw;
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await minioClient.BucketExistsAsync(bucketExistsArgs);
            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await minioClient.MakeBucketAsync(makeBucketArgs);
                logger.LogInformation("Created MinIO bucket {BucketName}", _bucketName);
            }
        }
    }
}