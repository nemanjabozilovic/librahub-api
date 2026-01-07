using LibraHub.Content.Domain.Access;
using LibraHub.Content.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Content.Infrastructure.Persistence.Configurations;

public class AccessGrantConfig : IEntityTypeConfiguration<AccessGrant>
{
    public void Configure(EntityTypeBuilder<AccessGrant> builder)
    {
        builder.ToTable("access_grants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.BookId)
            .IsRequired();

        builder.Property(x => x.Format)
            .HasConversion<int?>(
                v => v.HasValue ? (int?)v.Value : null,
                v => v.HasValue ? (BookFormat?)v.Value : null);

        builder.Property(x => x.Version);

        builder.Property(x => x.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired();

        builder.Property(x => x.RevokedAt);

        builder.HasIndex(x => x.Token)
            .IsUnique();
        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
