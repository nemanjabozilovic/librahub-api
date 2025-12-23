using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Options;
using Microsoft.Extensions.Options;
using Minio;

namespace LibraHub.BuildingBlocks.Storage;

public class MinioObjectStorage : IObjectStorage
{
    private readonly MinioClient _minioClient;
    private readonly StorageOptions _options;

    public MinioObjectStorage(MinioClient minioClient, IOptions<StorageOptions> options)
    {
        _minioClient = minioClient;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Ensure bucket exists
        var found = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName),
            cancellationToken);
        if (!found)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName),
                cancellationToken);
        }

        // Upload object
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
        var stream = new MemoryStream();

        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(async (s) =>
                {
                    await s.CopyToAsync(stream, cancellationToken);
                    stream.Position = 0;
                }),
            cancellationToken);

        return stream;
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

