using LibraHub.Content.Domain.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Content.Infrastructure.Persistence.Configurations;

public class StoredObjectConfig : IEntityTypeConfiguration<StoredObject>
{
    public void Configure(EntityTypeBuilder<StoredObject> builder)
    {
        builder.ToTable("stored_objects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.BookId)
            .IsRequired();

        builder.Property(x => x.ObjectKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SizeBytes)
            .IsRequired();

        builder.Property(x => x.Checksum)
            .HasConversion(
                v => v.Value,
                v => new Sha256(v))
            .HasColumnName("Checksum")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.UploadedAt)
            .IsRequired();

        builder.Property(x => x.BlockedAt);

        builder.Property(x => x.BlockReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => new { x.BookId, x.ObjectKey });
    }
}
