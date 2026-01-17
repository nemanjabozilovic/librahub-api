using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Commands.UpdateAnnouncement;

public record UpdateAnnouncementCommand(
    Guid AnnouncementId,
    Guid? BookId,
    string? Title,
    string? Content) : IRequest<Result>;
