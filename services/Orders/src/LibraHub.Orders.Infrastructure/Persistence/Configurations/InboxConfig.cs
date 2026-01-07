using LibraHub.BuildingBlocks.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Orders.Infrastructure.Persistence.Configurations;

public class InboxConfig : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EventType)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        builder.HasIndex(x => x.MessageId)
            .IsUnique();
    }
}
