using LibraHub.Notifications.Domain.Recipients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Notifications.Infrastructure.Persistence.Configurations;

public class UserNotificationSettingsConfig : IEntityTypeConfiguration<UserNotificationSettings>
{
    public void Configure(EntityTypeBuilder<UserNotificationSettings> builder)
    {
        builder.ToTable("user_notification_settings");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.IsStaff)
            .HasColumnName("is_staff")
            .IsRequired();

        builder.Property(x => x.EmailAnnouncementsEnabled)
            .HasColumnName("email_announcements_enabled")
            .IsRequired();

        builder.Property(x => x.EmailPromotionsEnabled)
            .HasColumnName("email_promotions_enabled")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.IsStaff);
        builder.HasIndex(x => x.EmailAnnouncementsEnabled);
        builder.HasIndex(x => x.EmailPromotionsEnabled);
    }
}

