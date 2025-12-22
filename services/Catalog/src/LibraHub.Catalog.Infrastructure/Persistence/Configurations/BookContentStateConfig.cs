using LibraHub.Catalog.Domain.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Catalog.Infrastructure.Persistence.Configurations;

public class BookContentStateConfig : IEntityTypeConfiguration<BookContentState>
{
    public void Configure(EntityTypeBuilder<BookContentState> builder)
    {
        builder.ToTable("book_content_state");

        builder.HasKey(s => s.BookId);

        builder.Property(s => s.BookId)
            .HasColumnName("book_id");

        builder.Property(s => s.HasCover)
            .HasColumnName("has_cover")
            .IsRequired();

        builder.Property(s => s.CoverRef)
            .HasColumnName("cover_ref")
            .HasMaxLength(500);

        builder.Property(s => s.HasEdition)
            .HasColumnName("has_edition")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
