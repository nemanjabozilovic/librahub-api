using LibraHub.BuildingBlocks.Inbox;
using LibraHub.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ApplicationAbstractions = LibraHub.Notifications.Application.Abstractions;

namespace LibraHub.Notifications.Infrastructure.Repositories;

public class InboxRepository : ApplicationAbstractions.IInboxRepository
{
    private readonly NotificationsDbContext _context;

    public InboxRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedMessages
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

        await _context.ProcessedMessages.AddAsync(processedMessage, cancellationToken);
    }
}
