using LibraHub.Catalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Catalog.Infrastructure.Persistence.Configurations;

public class BookConfig : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id");

        builder.Property(b => b.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasMaxLength(5000);

        builder.Property(b => b.Language)
            .HasColumnName("language")
            .HasMaxLength(50);

        builder.Property(b => b.Publisher)
            .HasColumnName("publisher")
            .HasMaxLength(200);

        builder.Property(b => b.PublicationDate)
            .HasColumnName("publication_date");

        builder.Property(b => b.Isbn)
            .HasColumnName("isbn")
            .HasMaxLength(17)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? new Isbn(v) : null);

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(b => b.RemovedBy)
            .HasColumnName("removed_by");

        builder.Property(b => b.RemovalReason)
            .HasColumnName("removal_reason")
            .HasMaxLength(1000);

        builder.Property(b => b.RemovedAt)
            .HasColumnName("removed_at");

        builder.HasIndex(b => b.Title);

        builder.HasMany(b => b.Authors)
            .WithOne()
            .HasForeignKey(a => a.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Categories)
            .WithOne()
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Tags)
            .WithOne()
            .HasForeignKey(t => t.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
