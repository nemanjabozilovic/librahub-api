using LibraHub.BuildingBlocks.Inbox;
using LibraHub.BuildingBlocks.Outbox;
using LibraHub.Catalog.Domain.Announcements;
using LibraHub.Catalog.Domain.Books;
using LibraHub.Catalog.Domain.Projections;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<BookAuthor> BookAuthors { get; set; } = null!;
    public DbSet<BookCategory> BookCategories { get; set; } = null!;
    public DbSet<BookTag> BookTags { get; set; } = null!;
    public DbSet<PricingPolicy> PricingPolicies { get; set; } = null!;
    public DbSet<Announcement> Announcements { get; set; } = null!;
    public DbSet<BookContentState> BookContentStates { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
