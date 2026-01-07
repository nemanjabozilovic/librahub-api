using LibraHub.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Orders.Infrastructure.Persistence.Configurations;

public class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(x => x.Subtotal, subtotal =>
        {
            subtotal.Property(s => s.Amount)
                .HasColumnName("subtotal_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            subtotal.Property(s => s.Currency)
                .HasColumnName("subtotal_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(x => x.VatTotal, vatTotal =>
        {
            vatTotal.Property(v => v.Amount)
                .HasColumnName("vat_total_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            vatTotal.Property(v => v.Currency)
                .HasColumnName("vat_total_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(x => x.Total, total =>
        {
            total.Property(t => t.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            total.Property(t => t.Currency)
                .HasColumnName("total_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(x => x.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.Status });
    }
}
