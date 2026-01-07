using LibraHub.Orders.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Orders.Infrastructure.Persistence.Configurations;

public class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(x => x.Amount, amount =>
        {
            amount.Property(a => a.Amount)
                .HasColumnName("amount_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            amount.Property(a => a.Currency)
                .HasColumnName("amount_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.ProviderReference)
            .HasColumnName("provider_reference")
            .HasMaxLength(200);

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.FailedAt)
            .HasColumnName("failed_at");

        builder.HasIndex(x => x.OrderId)
            .IsUnique();
        builder.HasIndex(x => x.ProviderReference);
        builder.HasIndex(x => x.Status);
    }
}
