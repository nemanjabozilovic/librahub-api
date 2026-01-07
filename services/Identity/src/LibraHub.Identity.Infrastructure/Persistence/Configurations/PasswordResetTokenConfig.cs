using LibraHub.Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Identity.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfig : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(prt => prt.Id);

        builder.Property(prt => prt.Id)
            .IsRequired();

        builder.Property(prt => prt.UserId)
            .IsRequired();

        builder.Property(prt => prt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(prt => prt.ExpiresAt)
            .IsRequired();

        builder.Property(prt => prt.CreatedAt)
            .IsRequired();

        builder.Property(prt => prt.UsedAt)
            .IsRequired(false);

        builder.HasIndex(prt => prt.Token)
            .IsUnique();

        builder.HasIndex(prt => prt.UserId);
    }
}
