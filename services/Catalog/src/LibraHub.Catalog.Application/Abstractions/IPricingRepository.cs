using LibraHub.Catalog.Domain.Books;

namespace LibraHub.Catalog.Application.Abstractions;

public interface IPricingRepository
{
    Task<PricingPolicy?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, PricingPolicy>> GetByBookIdsAsync(IEnumerable<Guid> bookIds, CancellationToken cancellationToken = default);

    Task AddAsync(PricingPolicy pricingPolicy, CancellationToken cancellationToken = default);

    Task UpdateAsync(PricingPolicy pricingPolicy, CancellationToken cancellationToken = default);
}
