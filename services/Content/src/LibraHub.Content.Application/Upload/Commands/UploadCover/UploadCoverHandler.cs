using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.BuildingBlocks.Storage;
using LibraHub.Content.Application.Abstractions;
using LibraHub.Content.Application.Options;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Errors;
using LibraHub.Content.Domain.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Content.Application.Upload.Commands.UploadCover;

public class UploadCoverHandler(
    IObjectStorage objectStorage,
    IStoredObjectRepository storedObjectRepository,
    ICoverRepository coverRepository,
    ICatalogReadClient catalogClient,
    IOutboxWriter outboxWriter,
    IClock clock,
    IOptions<UploadOptions> uploadOptions) : IRequestHandler<UploadCoverCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadCoverCommand request, CancellationToken cancellationToken)
    {
        var bookInfo = await catalogClient.GetBookInfoAsync(request.BookId, cancellationToken);
        if (bookInfo == null)
        {
            return Result.Failure<Guid>(Error.NotFound(ContentErrors.Book.NotFound));
        }

        if (bookInfo.IsBlocked)
        {
            return Result.Failure<Guid>(Error.Validation(ContentErrors.Book.Blocked));
        }

        var existingCover = await coverRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        if (existingCover != null)
        {
            var existingStoredObject = await storedObjectRepository.GetByIdAsync(existingCover.StoredObjectId, cancellationToken);
            if (existingStoredObject != null)
            {
                try
                {
                    await objectStorage.DeleteAsync(
                        uploadOptions.Value.CoversBucketName,
                        existingStoredObject.ObjectKey,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    return Result.Failure<Guid>(new Error("INTERNAL_ERROR", $"Failed to delete existing cover from storage: {ex.Message}"));
                }

                await coverRepository.DeleteAsync(existingCover, cancellationToken);
                await storedObjectRepository.DeleteAsync(existingStoredObject, cancellationToken);
            }
        }

        string sha256;
        using (var stream = request.File.OpenReadStream())
        {
            sha256 = await HashHelper.ComputeSha256Async(stream, cancellationToken);
        }

        var objectKey = $"books/{request.BookId}/cover/{Guid.NewGuid()}.{GetFileExtension(request.File.ContentType)}";

        try
        {
            await objectStorage.UploadAsync(
                uploadOptions.Value.CoversBucketName,
                objectKey,
                request.File.OpenReadStream(),
                request.File.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(new Error("INTERNAL_ERROR", $"{ContentErrors.Storage.UploadFailed}: {ex.Message}"));
        }

        var storedObject = new StoredObject(
            Guid.NewGuid(),
            request.BookId,
            objectKey,
            request.File.ContentType,
            request.File.Length,
            new Sha256(sha256));

        await storedObjectRepository.AddAsync(storedObject, cancellationToken);

        var cover = new Cover(
            Guid.NewGuid(),
            request.BookId,
            storedObject.Id);

        await coverRepository.AddAsync(cover, cancellationToken);
        await outboxWriter.WriteAsync(
            new Contracts.Content.V1.CoverUploadedV1
            {
                BookId = request.BookId,
                CoverRef = objectKey,
                Sha256 = sha256,
                Size = request.File.Length,
                ContentType = request.File.ContentType,
                UploadedAt = clock.UtcNowOffset
            },
            Contracts.Common.EventTypes.CoverUploaded,
            cancellationToken);

        return Result.Success(cover.Id);
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "bin"
        };
    }
}
