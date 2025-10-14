using Minio;
using Minio.DataModel.Args;

namespace SatOps.Modules.Operation
{
    public enum DataType
    {
        Telemetry,
        Command,
        Image
    }

    public interface IMinioService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, DataType dataType);
        Task<Stream> GetFileAsync(string objectPath);
        Task DeleteFileAsync(string objectPath);
    }

    public class MinioService(IMinioClient minioClient, IConfiguration configuration, ILogger<MinioService> logger) : IMinioService
    {
        private readonly string _bucketName = configuration.GetValue<string>("MinIO:BucketName") ?? "satops-data";

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, DataType dataType)
        {
            try
            {
                // Ensure bucket exists
                await EnsureBucketExistsAsync();

                // Generate unique object path based on data type
                var prefix = dataType switch
                {
                    DataType.Telemetry => "telemetry",
                    DataType.Command => "commands",
                    DataType.Image => "images",
                    _ => "data"
                };

                var objectPath = $"{prefix}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{fileName}";

                // Upload file
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectPath)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await minioClient.PutObjectAsync(putObjectArgs);

                logger.LogInformation("Successfully uploaded {DataType} file {FileName} to {ObjectPath}",
                    dataType, fileName, objectPath);
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
