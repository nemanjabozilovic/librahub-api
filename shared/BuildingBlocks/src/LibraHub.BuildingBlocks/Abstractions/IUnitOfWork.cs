using Microsoft.EntityFrameworkCore;

namespace LibraHub.BuildingBlocks.Abstractions;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork<TDbContext> : IUnitOfWork where TDbContext : DbContext
{
}
