using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Queries.GetAnnouncements;

public class GetAnnouncementsHandler(
    IAnnouncementRepository announcementRepository) : IRequestHandler<GetAnnouncementsQuery, Result<GetAnnouncementsResponseDto>>
{
    public async Task<Result<GetAnnouncementsResponseDto>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        List<Domain.Announcements.Announcement> announcements;
        int totalCount;

        if (request.BookId.HasValue)
        {
            announcements = await announcementRepository.GetByBookIdAsync(request.BookId.Value, request.Page, request.PageSize, cancellationToken);
            totalCount = await announcementRepository.CountByBookIdAsync(request.BookId.Value, cancellationToken);
        }
        else
        {
            announcements = await announcementRepository.GetPublishedAsync(request.Page, request.PageSize, cancellationToken);
            totalCount = await announcementRepository.CountPublishedAsync(cancellationToken);
        }

        var announcementDtos = announcements.Select(a => new AnnouncementDto
        {
            Id = a.Id,
            BookId = a.BookId,
            Title = a.Title,
            Content = a.Content,
            Status = a.Status.ToString(),
            PublishedAt = a.PublishedAt.HasValue ? new DateTimeOffset(a.PublishedAt.Value, TimeSpan.Zero) : null
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var response = new GetAnnouncementsResponseDto
        {
            Announcements = announcementDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };

        return Result.Success(response);
    }
}
