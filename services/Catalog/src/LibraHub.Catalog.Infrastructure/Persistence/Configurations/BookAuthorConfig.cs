using LibraHub.Catalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Catalog.Infrastructure.Persistence.Configurations;

public class BookAuthorConfig : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("book_authors");

        builder.HasKey(a => new { a.BookId, a.Name });

        builder.Property(a => a.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
    }
}
