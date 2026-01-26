using LibraHub.Catalog.Domain.Announcements;

namespace LibraHub.Catalog.Application.Abstractions;

public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Announcement>> GetByBookIdAsync(Guid bookId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<List<Announcement>> GetPublishedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountPublishedAsync(CancellationToken cancellationToken = default);

    Task<List<Announcement>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Announcement announcement, CancellationToken cancellationToken = default);

    Task UpdateAsync(Announcement announcement, CancellationToken cancellationToken = default);

    Task DeleteAsync(Announcement announcement, CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(IEnumerable<Announcement> announcements, CancellationToken cancellationToken = default);
}
