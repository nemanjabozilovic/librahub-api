using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Announcements.Commands.UpdateAnnouncement;

public class UpdateAnnouncementHandler(
    IAnnouncementRepository announcementRepository) : IRequestHandler<UpdateAnnouncementCommand, Result>
{
    public async Task<Result> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await announcementRepository.GetByIdAsync(request.AnnouncementId, cancellationToken);
        if (announcement == null)
        {
            return Result.Failure(Error.NotFound(CatalogErrors.Announcement.NotFound));
        }

        var titleChanged = !string.IsNullOrWhiteSpace(request.Title) && request.Title != announcement.Title;
        var contentChanged = !string.IsNullOrWhiteSpace(request.Content) && request.Content != announcement.Content;
        var bookIdChanged = request.BookId != announcement.BookId;

        if (!titleChanged && !contentChanged && !bookIdChanged)
        {
            return Result.Failure(Error.Validation("At least one field (Title, Content, or BookId) must be provided"));
        }

        try
        {
            var newTitle = !string.IsNullOrWhiteSpace(request.Title) ? request.Title : announcement.Title;
            var newContent = !string.IsNullOrWhiteSpace(request.Content) ? request.Content : announcement.Content;

            announcement.Update(newTitle, newContent, announcement.ImageRef);

            if (bookIdChanged)
            {
                announcement.UpdateBookId(request.BookId);
            }

            await announcementRepository.UpdateAsync(announcement, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        return Result.Success();
    }
}
