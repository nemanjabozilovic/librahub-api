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

namespace LibraHub.Content.Application.Upload.Commands.UploadEdition;

public class UploadEditionHandler(
    IObjectStorage objectStorage,
    IStoredObjectRepository storedObjectRepository,
    IBookEditionRepository editionRepository,
    ICatalogReadClient catalogClient,
    IOutboxWriter outboxWriter,
    IClock clock,
    IOptions<UploadOptions> uploadOptions) : IRequestHandler<UploadEditionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UploadEditionCommand request, CancellationToken cancellationToken)
    {
        var bookInfoResult = await ValidateBookInfoAsync(request.BookId, cancellationToken);
        if (bookInfoResult.IsFailure)
        {
            return Result.Failure<Guid>(bookInfoResult.Error!);
        }

        if (!Enum.TryParse<BookFormat>(request.Format, ignoreCase: true, out var format))
        {
            return Result.Failure<Guid>(Error.Validation(ContentErrors.Edition.InvalidFormat));
        }

        var latest = await editionRepository.GetLatestByBookIdAndFormatAsync(request.BookId, format, cancellationToken);
        var version = latest != null ? latest.Version + 1 : 1;

        string sha256;
        using (var stream = request.File.OpenReadStream())
        {
            sha256 = await HashHelper.ComputeSha256Async(stream, cancellationToken);
        }

        var fileExtension = format == BookFormat.Pdf ? "pdf" : "epub";
        var objectKey = $"books/{request.BookId}/editions/{format.ToString().ToLowerInvariant()}/v{version}/{Guid.NewGuid()}.{fileExtension}";

        try
        {
            await objectStorage.UploadAsync(
                uploadOptions.Value.EditionsBucketName,
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

        var edition = new BookEdition(
            Guid.NewGuid(),
            request.BookId,
            format,
            version,
            storedObject.Id);

        await editionRepository.AddAsync(edition, cancellationToken);
        await outboxWriter.WriteAsync(
            new Contracts.Content.V1.EditionUploadedV1
            {
                BookId = request.BookId,
                Format = format.ToString(),
                Version = version,
                EditionRef = objectKey,
                Sha256 = sha256,
                Size = request.File.Length,
                ContentType = request.File.ContentType,
                UploadedAt = clock.UtcNowOffset
            },
            Contracts.Common.EventTypes.EditionUploaded,
            cancellationToken);

        return Result.Success(edition.Id);
    }

    private async Task<Result> ValidateBookInfoAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var bookInfoResult = await catalogClient.GetBookInfoAsync(bookId, cancellationToken);
        if (bookInfoResult.IsFailure)
        {
            return Result.Failure(bookInfoResult.Error ?? Error.NotFound(ContentErrors.Book.NotFound));
        }

        var bookInfo = bookInfoResult.Value;

        if (bookInfo.IsBlocked)
        {
            return Result.Failure(Error.Validation(ContentErrors.Book.Blocked));
        }

        return Result.Success();
    }
}
