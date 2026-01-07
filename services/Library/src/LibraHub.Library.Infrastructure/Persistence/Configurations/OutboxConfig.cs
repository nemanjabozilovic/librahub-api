using LibraHub.BuildingBlocks.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Library.Infrastructure.Persistence.Configurations;

public class OutboxConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("EventType")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("Payload")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("ProcessedAt");

        builder.Property(x => x.Error)
            .HasColumnName("Error")
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
    }
}
