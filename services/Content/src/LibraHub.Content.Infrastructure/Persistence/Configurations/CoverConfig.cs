using LibraHub.Content.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Content.Infrastructure.Persistence.Configurations;

public class CoverConfig : IEntityTypeConfiguration<Cover>
{
    public void Configure(EntityTypeBuilder<Cover> builder)
    {
        builder.ToTable("covers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.BookId)
            .IsRequired();

        builder.Property(x => x.StoredObjectId)
            .IsRequired();

        builder.Property(x => x.UploadedAt)
            .IsRequired();

        builder.Property(x => x.IsBlocked)
            .IsRequired();

        builder.Property(x => x.BlockedAt);

        builder.HasIndex(x => x.BookId)
            .IsUnique();
    }
}
