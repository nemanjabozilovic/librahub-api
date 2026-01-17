using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Library.Application.Abstractions;
using LibraHub.Library.Domain.Errors;
using LibraHub.Library.Domain.Reading;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Library.Application.Reading.Commands.UpdateProgress;

public class UpdateProgressHandler(
    IReadingProgressRepository progressRepository,
    IEntitlementRepository entitlementRepository,
    ICurrentUser currentUser) : IRequestHandler<UpdateProgressCommand, Result>
{
    public async Task<Result> Handle(UpdateProgressCommand request, CancellationToken cancellationToken)
    {
        var userIdResult = currentUser.RequireUserId(LibraryErrors.User.NotAuthenticated);
        if (userIdResult.IsFailure)
        {
            return Result.Failure(userIdResult.Error!);
        }

        var userId = userIdResult.Value;

        var accessResult = await ValidateUserAccessAsync(userId, request.BookId, cancellationToken);
        if (accessResult.IsFailure)
        {
            return accessResult;
        }

        var normalizedFormat = request.Format?.ToUpperInvariant();
        var progress = await progressRepository.GetByUserBookFormatAndVersionAsync(
            userId, request.BookId, normalizedFormat, request.Version, cancellationToken);

        if (progress == null)
        {
            progress = new ReadingProgress(
                Guid.NewGuid(),
                userId,
                request.BookId,
                normalizedFormat,
                request.Version);
            progress.UpdateProgress(request.Percentage, request.LastPage);
            await progressRepository.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.UpdateProgress(request.Percentage, request.LastPage);
            await progressRepository.UpdateAsync(progress, cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result> ValidateUserAccessAsync(Guid userId, Guid bookId, CancellationToken cancellationToken)
    {
        bool hasAccess = currentUser.IsInRole("Admin") || currentUser.IsInRole("Librarian");
        if (!hasAccess)
        {
            hasAccess = await entitlementRepository.HasAccessAsync(userId, bookId, cancellationToken);
        }

        if (!hasAccess)
        {
            return Result.Failure(Error.Validation("User does not have access to this book"));
        }

        return Result.Success();
    }
}
