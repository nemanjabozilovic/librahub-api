using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using MediatR;

namespace LibraHub.Catalog.Application.Announcements.Queries.GetAnnouncements;

public class GetAnnouncementsHandler(
    IAnnouncementRepository announcementRepository) : IRequestHandler<GetAnnouncementsQuery, Result<GetAnnouncementsResponseDto>>
{
    public async Task<Result<GetAnnouncementsResponseDto>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        Task<List<Domain.Announcements.Announcement>> announcementsTask;
        Task<int> totalCountTask;

        if (request.BookId.HasValue)
        {
            announcementsTask = announcementRepository.GetByBookIdAsync(request.BookId.Value, request.Page, request.PageSize, cancellationToken);
            totalCountTask = announcementRepository.CountByBookIdAsync(request.BookId.Value, cancellationToken);
        }
        else
        {
            announcementsTask = announcementRepository.GetPublishedAsync(request.Page, request.PageSize, cancellationToken);
            totalCountTask = announcementRepository.CountPublishedAsync(cancellationToken);
        }

        await Task.WhenAll(announcementsTask, totalCountTask);

        var announcements = await announcementsTask;
        var totalCount = await totalCountTask;

        var announcementDtos = announcements.Select(a => new AnnouncementDto
        {
            Id = a.Id,
            BookId = a.BookId,
            Title = a.Title,
            Content = a.Content,
            Status = a.Status.ToString(),
            PublishedAt = a.PublishedAt
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
