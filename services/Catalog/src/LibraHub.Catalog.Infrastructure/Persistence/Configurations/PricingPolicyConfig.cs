using LibraHub.Catalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Catalog.Infrastructure.Persistence.Configurations;

public class PricingPolicyConfig : IEntityTypeConfiguration<PricingPolicy>
{
    public void Configure(EntityTypeBuilder<PricingPolicy> builder)
    {
        builder.ToTable("pricing_policies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("price")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.VatRate)
            .HasColumnName("vat_rate")
            .HasPrecision(5, 2);

        builder.OwnsOne(p => p.PromoPrice, promo =>
        {
            promo.Property(m => m.Amount)
                .HasColumnName("promo_price")
                .HasPrecision(18, 2);

            promo.Property(m => m.Currency)
                .HasColumnName("promo_currency")
                .HasMaxLength(3);
        });

        builder.Property(p => p.PromoStartDate)
            .HasColumnName("promo_start_date");

        builder.Property(p => p.PromoEndDate)
            .HasColumnName("promo_end_date");

        builder.Property(p => p.PromoName)
            .HasColumnName("promo_name")
            .HasMaxLength(200);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(p => p.BookId)
            .IsUnique();
    }
}
