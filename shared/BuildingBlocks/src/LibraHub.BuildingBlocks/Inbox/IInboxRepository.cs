namespace LibraHub.BuildingBlocks.Inbox;

public interface IInboxRepository
{
    Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(string messageId, string eventType, CancellationToken cancellationToken = default);
}
