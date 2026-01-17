using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Options;
using Microsoft.Extensions.Options;
using Minio;

namespace LibraHub.BuildingBlocks.Storage;

public class MinioObjectStorage(MinioClient minioClient, IOptions<StorageOptions> options) : IObjectStorage
{
    private readonly MinioClient _minioClient = minioClient;
    private readonly StorageOptions _options = options.Value;

    public async Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var found = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName),
            cancellationToken);
        if (!found)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName),
                cancellationToken);
        }

        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType(contentType),
            cancellationToken);

        return objectKey;
    }

    public async Task<Stream> DownloadAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        try
        {
            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(sourceStream =>
                    {
                        sourceStream.CopyTo(memoryStream);
                    }),
                cancellationToken);

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch
        {
            memoryStream.Dispose();
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey),
                cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey),
            cancellationToken);
    }
}
