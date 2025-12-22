using LibraHub.Catalog.Domain.Projections;

namespace LibraHub.Catalog.Application.Abstractions;

public interface IBookContentStateRepository
{
    Task<BookContentState?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(BookContentState state, CancellationToken cancellationToken = default);

    Task UpdateAsync(BookContentState state, CancellationToken cancellationToken = default);
}
