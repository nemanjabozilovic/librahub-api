using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Announcements.Commands.UploadAnnouncementImage;

public class UploadAnnouncementImageHandler(
    IAnnouncementRepository announcementRepository,
    IObjectStorage objectStorage,
    IOptions<UploadOptions> uploadOptions,
    IOptions<CatalogOptions> catalogOptions,
    ILogger<UploadAnnouncementImageHandler> logger) : IRequestHandler<UploadAnnouncementImageCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UploadAnnouncementImageCommand request, CancellationToken cancellationToken)
    {
        var announcement = await announcementRepository.GetByIdAsync(request.AnnouncementId, cancellationToken);
        if (announcement == null)
        {
            return Result.Failure<string>(Error.NotFound(CatalogErrors.Announcement.NotFound));
        }

        if (announcement.Status != Domain.Announcements.AnnouncementStatus.Draft)
        {
            return Result.Failure<string>(Error.Validation("Can only upload image for draft announcements"));
        }

        if (!string.IsNullOrWhiteSpace(announcement.ImageRef))
        {
            try
            {
                await objectStorage.DeleteAsync(
                    uploadOptions.Value.AnnouncementImagesBucketName,
                    announcement.ImageRef,
                    cancellationToken);
                logger.LogInformation("Deleted old announcement image: {ImageRef} for AnnouncementId: {AnnouncementId}", announcement.ImageRef, announcement.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old announcement image: {ImageRef} for AnnouncementId: {AnnouncementId}. Continuing with upload.", announcement.ImageRef, announcement.Id);
            }
        }

        var fileExtension = GetFileExtension(request.File.ContentType);
        var objectKey = $"announcements/{request.AnnouncementId}/{Guid.NewGuid()}.{fileExtension}";

        try
        {
            using var stream = request.File.OpenReadStream();
            await objectStorage.UploadAsync(
                uploadOptions.Value.AnnouncementImagesBucketName,
                objectKey,
                stream,
                request.File.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Validation($"Failed to upload image: {ex.Message}"));
        }

        announcement.SetImage(objectKey);
        await announcementRepository.UpdateAsync(announcement, cancellationToken);

        var imageUrl = $"{catalogOptions.Value.GatewayBaseUrl}/api/announcement-images/{objectKey}";

        return Result.Success(imageUrl);
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "bin"
        };
    }
}

