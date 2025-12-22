using LibraHub.Catalog.Domain.Books;

namespace LibraHub.Catalog.Application.Abstractions;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Book>> SearchAsync(string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountSearchAsync(string? searchTerm, CancellationToken cancellationToken = default);

    Task AddAsync(Book book, CancellationToken cancellationToken = default);

    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
}
