using LibraHub.BuildingBlocks.Messaging;
using LibraHub.Content.Application.Consumers;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Contracts.Common;
using RabbitMQ.Client;

namespace LibraHub.Content.Api.Workers;

public class ContentEventConsumerWorker : EventConsumerWorker
{
    public ContentEventConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<ContentEventConsumerWorker> logger,
        IConnection connection)
        : base(serviceProvider, logger, connection, "content-events", "librahub.events")
    {
    }

    protected override IEnumerable<string> GetSubscribedEventTypes()
    {
        return new[]
        {
            EventTypes.BookRemoved
        };
    }

    protected override async Task HandleEventAsync(string eventType, string payload, IServiceScope scope, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case EventTypes.BookRemoved:
                var bookRemovedEvent = DeserializeEvent<BookRemovedV1>(payload);
                if (bookRemovedEvent != null)
                {
                    var bookRemovedConsumer = scope.ServiceProvider.GetRequiredService<BookRemovedConsumer>();
                    await bookRemovedConsumer.HandleAsync(bookRemovedEvent, cancellationToken);
                }
                break;
        }
    }
}

