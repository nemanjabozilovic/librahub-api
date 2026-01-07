using LibraHub.BuildingBlocks.Inbox;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.Library.Domain.Books;
using LibraHub.Library.Domain.Entitlements;
using LibraHub.Library.Domain.Reading;
using LibraHub.Library.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Infrastructure.Persistence;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<Entitlement> Entitlements { get; set; } = null!;
    public DbSet<BookSnapshot> BookSnapshots { get; set; } = null!;
    public DbSet<ReadingProgress> ReadingProgress { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
