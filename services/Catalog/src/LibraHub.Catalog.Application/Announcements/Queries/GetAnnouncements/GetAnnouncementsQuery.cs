using LibraHub.BuildingBlocks.Results;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Queries.GetAnnouncements;

public record GetAnnouncementsQuery(
    Guid? BookId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<GetAnnouncementsResponseDto>>;
