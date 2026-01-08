using LibraHub.Catalog.Domain.Announcements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Catalog.Infrastructure.Persistence.Configurations;

public class AnnouncementConfig : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.BookId)
            .HasColumnName("book_id")
            .IsRequired(false);

        builder.Property(a => a.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.Content)
            .HasColumnName("content")
            .HasMaxLength(10000)
            .IsRequired();

        builder.Property(a => a.ImageRef)
            .HasColumnName("image_ref")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(a => a.BookId);
        builder.HasIndex(a => a.Status);

        builder.HasIndex(a => new { a.Status, a.PublishedAt })
            .HasFilter("\"status\" = 1");
    }
}
