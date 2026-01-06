using LibraHub.BuildingBlocks.Inbox;
using LibraHub.Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibraHub.Library.Infrastructure.Repositories;

public class InboxRepository(LibraryDbContext context) : IInboxRepository
{
    public async Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await context.ProcessedMessages
            .AnyAsync(p => p.MessageId == messageId, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(string messageId, string eventType, CancellationToken cancellationToken = default)
    {
        var processedMessage = new ProcessedMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        };

        await context.ProcessedMessages.AddAsync(processedMessage, cancellationToken);
    }
}
