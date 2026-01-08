using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementHandler(
    IAnnouncementRepository announcementRepository,
    IObjectStorage objectStorage,
    IOptions<UploadOptions> uploadOptions,
    ILogger<DeleteAnnouncementHandler> logger) : IRequestHandler<DeleteAnnouncementCommand, Result>
{
    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await announcementRepository.GetByIdAsync(request.AnnouncementId, cancellationToken);
        if (announcement == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Announcement.NotFound));
        }

        if (!string.IsNullOrWhiteSpace(announcement.ImageRef))
        {
            try
            {
                await objectStorage.DeleteAsync(
                    uploadOptions.Value.AnnouncementImagesBucketName,
                    announcement.ImageRef,
                    cancellationToken);
                logger.LogInformation("Deleted announcement image: {ImageRef} for AnnouncementId: {AnnouncementId}", announcement.ImageRef, announcement.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete announcement image: {ImageRef} for AnnouncementId: {AnnouncementId}. Continuing with announcement deletion.", announcement.ImageRef, announcement.Id);
            }
        }

        await announcementRepository.DeleteAsync(announcement, cancellationToken);

        logger.LogInformation("Deleted announcement: {AnnouncementId}", announcement.Id);

        return Result.Success();
    }
}

