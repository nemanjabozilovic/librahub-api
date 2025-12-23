using LibraHub.BuildingBlocks.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Notifications.Infrastructure.Persistence.Configurations;

public class OutboxConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(1000);

        builder.HasIndex(x => x.ProcessedAt);
        builder.HasIndex(x => x.CreatedAt);
    }
}

