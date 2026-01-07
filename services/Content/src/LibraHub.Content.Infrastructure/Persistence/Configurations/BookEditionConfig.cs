using LibraHub.Content.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Content.Infrastructure.Persistence.Configurations;

public class BookEditionConfig : IEntityTypeConfiguration<BookEdition>
{
    public void Configure(EntityTypeBuilder<BookEdition> builder)
    {
        builder.ToTable("book_editions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.BookId)
            .IsRequired();

        builder.Property(x => x.Format)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.StoredObjectId)
            .IsRequired();

        builder.Property(x => x.UploadedAt)
            .IsRequired();

        builder.Property(x => x.IsBlocked)
            .IsRequired();

        builder.Property(x => x.BlockedAt);

        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => new { x.BookId, x.Format, x.Version })
            .IsUnique();
    }
}
