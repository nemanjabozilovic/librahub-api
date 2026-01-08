using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;

public record DeleteAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;

