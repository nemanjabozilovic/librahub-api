using LibraHub.Library.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Library.Infrastructure.Persistence.Configurations;

public class BookSnapshotConfig : IEntityTypeConfiguration<BookSnapshot>
{
    public void Configure(EntityTypeBuilder<BookSnapshot> builder)
    {
        builder.ToTable("book_snapshots");

        builder.HasKey(x => x.BookId);

        builder.Property(x => x.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Authors)
            .HasColumnName("authors")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.CoverRef)
            .HasColumnName("cover_ref")
            .HasMaxLength(500);

        builder.Property(x => x.Availability)
            .HasColumnName("availability")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PriceLabel)
            .HasColumnName("price_label")
            .HasMaxLength(50);

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.Availability);
    }
}
