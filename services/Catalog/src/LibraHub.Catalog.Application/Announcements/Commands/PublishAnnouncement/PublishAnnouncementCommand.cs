using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Commands.PublishAnnouncement;

public record PublishAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;
