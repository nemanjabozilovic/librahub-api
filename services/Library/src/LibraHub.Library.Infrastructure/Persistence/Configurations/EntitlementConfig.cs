using LibraHub.Library.Domain.Entitlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Library.Infrastructure.Persistence.Configurations;

public class EntitlementConfig : IEntityTypeConfiguration<Entitlement>
{
    public void Configure(EntityTypeBuilder<Entitlement> builder)
    {
        builder.ToTable("entitlements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AcquiredAt)
            .HasColumnName("acquired_at")
            .IsRequired();

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(x => x.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(500);

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id");

        // Unique constraint on (UserId, BookId)
        builder.HasIndex(x => new { x.UserId, x.BookId })
            .IsUnique();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AcquiredAt);
    }
}
