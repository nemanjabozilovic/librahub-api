using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Application.Options;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Microsoft.Extensions.Options;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Announcements.Commands.PublishAnnouncement;

public class PublishAnnouncementHandler(
    IAnnouncementRepository announcementRepository,
    IOutboxWriter outboxWriter,
    IOptions<CatalogOptions> catalogOptions) : IRequestHandler<PublishAnnouncementCommand, Result>
{
    public async Task<Result> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await announcementRepository.GetByIdAsync(request.AnnouncementId, cancellationToken);
        if (announcement == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Announcement.NotFound));
        }

        try
        {
            announcement.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await announcementRepository.UpdateAsync(announcement, cancellationToken);

        var imageUrl = !string.IsNullOrWhiteSpace(announcement.ImageRef)
            ? $"{catalogOptions.Value.GatewayBaseUrl}/api/announcement-images/{announcement.ImageRef}"
            : null;

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.AnnouncementPublishedV1
            {
                AnnouncementId = announcement.Id,
                BookId = announcement.BookId,
                Title = announcement.Title,
                Content = announcement.Content,
                ImageUrl = imageUrl,
                PublishedAt = new DateTimeOffset(announcement.PublishedAt!.Value, TimeSpan.Zero)
            },
            Contracts.Common.EventTypes.AnnouncementPublished,
            cancellationToken);

        return Result.Success();
    }
}
