using LibraHub.Content.Domain.Books;

namespace LibraHub.Content.Application.Abstractions;

public interface ICoverRepository
{
    Task<Cover?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(Cover cover, CancellationToken cancellationToken = default);

    Task UpdateAsync(Cover cover, CancellationToken cancellationToken = default);

    Task DeleteAsync(Cover cover, CancellationToken cancellationToken = default);
}
