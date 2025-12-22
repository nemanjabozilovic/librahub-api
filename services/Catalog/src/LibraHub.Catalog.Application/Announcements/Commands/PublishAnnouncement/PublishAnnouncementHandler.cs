using LibraHub.BuildingBlocks.Abstractions;
using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Announcements.Commands.PublishAnnouncement;

public class PublishAnnouncementHandler(
    IAnnouncementRepository announcementRepository,
    IOutboxWriter outboxWriter) : IRequestHandler<PublishAnnouncementCommand, Result>
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

        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.AnnouncementPublishedV1
            {
                AnnouncementId = announcement.Id,
                BookId = announcement.BookId,
                Title = announcement.Title,
                PublishedAt = announcement.PublishedAt!.Value
            },
            Contracts.Common.EventTypes.AnnouncementPublished,
            cancellationToken);

        return Result.Success();
    }
}
