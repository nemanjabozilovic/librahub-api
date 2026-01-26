using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementHandler(
    IAnnouncementRepository announcementRepository,
    IObjectStorage objectStorage,
    IOptions<UploadOptions> uploadOptions,
    IUnitOfWork unitOfWork,
    ILogger<DeleteAnnouncementHandler> logger) : IRequestHandler<DeleteAnnouncementCommand, Result>
{
    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        if (request.AnnouncementIds == null || request.AnnouncementIds.Count == 0)
        {
            return Result.Failure(Error.Validation("At least one announcement ID is required"));
        }

        var announcements = new List<Domain.Announcements.Announcement>();

        foreach (var announcementId in request.AnnouncementIds)
        {
            var announcement = await announcementRepository.GetByIdAsync(announcementId, cancellationToken);
            if (announcement == null)
            {
                return Result.Failure(Error.NotFound($"Announcement with ID {announcementId} not found"));
            }

            announcements.Add(announcement);
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var announcement in announcements)
            {
                if (!string.IsNullOrWhiteSpace(announcement.ImageRef))
                {
                    try
                    {
                        await objectStorage.DeleteAsync(
                            uploadOptions.Value.AnnouncementImagesBucketName,
                            announcement.ImageRef,
                            ct);
                        logger.LogInformation("Deleted announcement image: {ImageRef} for AnnouncementId: {AnnouncementId}", announcement.ImageRef, announcement.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete announcement image: {ImageRef} for AnnouncementId: {AnnouncementId}. Continuing with announcement deletion.", announcement.ImageRef, announcement.Id);
                    }
                }
            }

            await announcementRepository.DeleteRangeAsync(announcements, ct);
            logger.LogInformation("Deleted {Count} announcement(s)", announcements.Count);
        }, cancellationToken);

        return Result.Success();
    }
}
