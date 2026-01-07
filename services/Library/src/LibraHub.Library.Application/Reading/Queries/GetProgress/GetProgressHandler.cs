using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Library.Application.Reading.Queries.GetProgress;

public class GetProgressHandler(
    IReadingProgressRepository progressRepository,
    ICurrentUser currentUser) : IRequestHandler<GetProgressQuery, Result<ReadingProgressDto>>
{
    public async Task<Result<ReadingProgressDto>> Handle(GetProgressQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result.Failure<ReadingProgressDto>(Error.Unauthorized(LibraryErrors.User.NotAuthenticated));
        }

        var userId = currentUser.UserId.Value;

        var normalizedFormat = request.Format?.ToUpperInvariant();
        var progress = await progressRepository.GetByUserBookFormatAndVersionAsync(
            userId, request.BookId, normalizedFormat, request.Version, cancellationToken);

        if (progress == null)
        {
            return Result.Success(new ReadingProgressDto
            {
                BookId = request.BookId,
                Format = normalizedFormat,
                Version = request.Version,
                Percentage = 0,
                LastPage = null,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });
        }

        return Result.Success(new ReadingProgressDto
        {
            BookId = progress.BookId,
            Format = progress.Format,
            Version = progress.Version,
            Percentage = progress.ProgressPercentage,
            LastPage = progress.LastPage,
            LastUpdatedAt = new DateTimeOffset(progress.LastUpdatedAt, TimeSpan.Zero)
        });
    }
}
