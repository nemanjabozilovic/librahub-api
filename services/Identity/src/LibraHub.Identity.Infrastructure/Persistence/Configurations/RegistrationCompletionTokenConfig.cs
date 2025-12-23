using LibraHub.Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Identity.Infrastructure.Persistence.Configurations;

public class RegistrationCompletionTokenConfig : IEntityTypeConfiguration<RegistrationCompletionToken>
{
    public void Configure(EntityTypeBuilder<RegistrationCompletionToken> builder)
    {
        builder.ToTable("RegistrationCompletionTokens");

        builder.HasKey(rct => rct.Id);

        builder.Property(rct => rct.Id)
            .IsRequired();

        builder.Property(rct => rct.UserId)
            .IsRequired();

        builder.Property(rct => rct.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rct => rct.ExpiresAt)
            .IsRequired();

        builder.Property(rct => rct.CreatedAt)
            .IsRequired();

        builder.Property(rct => rct.UsedAt)
            .IsRequired(false);

        builder.HasIndex(rct => rct.Token)
            .IsUnique();

        builder.HasIndex(rct => rct.UserId);
    }
}

