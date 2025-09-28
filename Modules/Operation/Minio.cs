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

    public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly ILogger<MinioService> _logger;

        public MinioService(IMinioClient minioClient, IConfiguration configuration, ILogger<MinioService> logger)
        {
            _minioClient = minioClient;
            _bucketName = configuration.GetValue<string>("MinIO:BucketName") ?? "satops-data";
            _logger = logger;
        }

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

                await _minioClient.PutObjectAsync(putObjectArgs);

                _logger.LogInformation("Successfully uploaded {DataType} file {FileName} to {ObjectPath}",
                    dataType, fileName, objectPath);
                return objectPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload {DataType} file {FileName} to MinIO", dataType, fileName);
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

                await _minioClient.GetObjectAsync(getObjectArgs);

                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file {ObjectPath} from MinIO", objectPath);
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

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                _logger.LogInformation("Successfully deleted file {ObjectPath} from MinIO", objectPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {ObjectPath} from MinIO", objectPath);
                throw;
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation("Created MinIO bucket {BucketName}", _bucketName);
            }
        }
    }
}
