using LibraHub.BuildingBlocks.Inbox;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.Content.Domain.Access;
using LibraHub.Content.Domain.Books;
using LibraHub.Content.Domain.Storage;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Content.Infrastructure.Persistence;

public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    public DbSet<StoredObject> StoredObjects { get; set; } = null!;
    public DbSet<BookEdition> BookEditions { get; set; } = null!;
    public DbSet<Cover> Covers { get; set; } = null!;
    public DbSet<AccessGrant> AccessGrants { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
