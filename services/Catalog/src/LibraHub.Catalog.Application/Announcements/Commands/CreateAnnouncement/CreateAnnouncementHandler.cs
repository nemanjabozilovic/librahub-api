using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Announcements;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementHandler(
    IAnnouncementRepository announcementRepository) : IRequestHandler<CreateAnnouncementCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = new Announcement(Guid.NewGuid(), request.BookId, request.Title, request.Content);
        await announcementRepository.AddAsync(announcement, cancellationToken);
        return Result.Success(announcement.Id);
    }
}
