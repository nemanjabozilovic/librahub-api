using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Commands.CreateAnnouncement;

public record CreateAnnouncementCommand(
    Guid? BookId,
    string Title,
    string Content) : IRequest<Result<Guid>>;
