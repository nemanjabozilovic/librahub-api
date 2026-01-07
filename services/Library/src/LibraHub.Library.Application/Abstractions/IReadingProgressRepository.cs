using LibraHub.Library.Domain.Reading;

namespace LibraHub.Library.Application.Abstractions;

public interface IReadingProgressRepository
{
    Task<ReadingProgress?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);

    Task<ReadingProgress?> GetByUserBookFormatAndVersionAsync(
        Guid userId,
        Guid bookId,
        string? format,
        int? version,
        CancellationToken cancellationToken = default);

    Task AddAsync(ReadingProgress progress, CancellationToken cancellationToken = default);

    Task UpdateAsync(ReadingProgress progress, CancellationToken cancellationToken = default);
}
