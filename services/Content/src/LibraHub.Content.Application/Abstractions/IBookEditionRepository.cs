using LibraHub.Content.Domain.Books;

namespace LibraHub.Content.Application.Abstractions;

public interface IBookEditionRepository
{
    Task<BookEdition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BookEdition?> GetByBookIdFormatAndVersionAsync(Guid bookId, BookFormat format, int version, CancellationToken cancellationToken = default);

    Task<BookEdition?> GetLatestByBookIdAndFormatAsync(Guid bookId, BookFormat format, CancellationToken cancellationToken = default);

    Task<List<BookEdition>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<List<BookEdition>> GetByBookIdsAsync(List<Guid> bookIds, CancellationToken cancellationToken = default);

    Task AddAsync(BookEdition edition, CancellationToken cancellationToken = default);

    Task UpdateAsync(BookEdition edition, CancellationToken cancellationToken = default);

    Task DeleteAsync(BookEdition edition, CancellationToken cancellationToken = default);
}
