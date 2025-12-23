using LibraHub.BuildingBlocks.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Notifications.Infrastructure.Persistence.Configurations;

public class InboxConfig : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();

        builder.HasIndex(x => x.MessageId)
            .IsUnique();
    }
}

