using LibraHub.BuildingBlocks.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraHub.Library.Infrastructure.Persistence.Configurations;

public class InboxConfig : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(x => x.MessageId)
            .HasColumnName("MessageId")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasColumnName("EventType")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("ProcessedAt")
            .IsRequired();

        builder.HasIndex(x => x.MessageId)
            .IsUnique();
    }
}
