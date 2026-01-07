using LibraHub.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Orders.Infrastructure.Persistence.Configurations;

public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(x => x.BookTitle)
            .HasColumnName("book_title")
            .HasMaxLength(500)
            .IsRequired();

        builder.OwnsOne(x => x.BasePrice, basePrice =>
        {
            basePrice.Property(b => b.Amount)
                .HasColumnName("base_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            basePrice.Property(b => b.Currency)
                .HasColumnName("base_price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(x => x.FinalPrice, finalPrice =>
        {
            finalPrice.Property(f => f.Amount)
                .HasColumnName("final_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            finalPrice.Property(f => f.Currency)
                .HasColumnName("final_price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.VatRate)
            .HasColumnName("vat_rate")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.OwnsOne(x => x.VatAmount, vatAmount =>
        {
            vatAmount.Property(v => v.Amount)
                .HasColumnName("vat_amount_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            vatAmount.Property(v => v.Currency)
                .HasColumnName("vat_amount_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.PromotionId)
            .HasColumnName("promotion_id");

        builder.Property(x => x.PromotionName)
            .HasColumnName("promotion_name")
            .HasMaxLength(200);

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.BookId);
    }
}
