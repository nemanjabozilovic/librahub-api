using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;

public record DeleteAnnouncementCommand(List<Guid> AnnouncementIds) : IRequest<Result>;
