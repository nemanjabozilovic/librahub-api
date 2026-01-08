using LibraHub.BuildingBlocks.Messaging;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Library.Application.Consumers;
using RabbitMQ.Client;

namespace LibraHub.Library.Api.Workers;

public class LibraryEventConsumerWorker : EventConsumerWorker
{
    public LibraryEventConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<LibraryEventConsumerWorker> logger,
        IConnection connection)
        : base(serviceProvider, logger, connection, "library-events", "librahub.events")
    {
    }

    protected override IEnumerable<string> GetSubscribedEventTypes()
    {
        return new[]
        {
            EventTypes.BookPublished,
            EventTypes.BookUpdated,
            EventTypes.BookRemoved,
            EventTypes.OrderPaid,
            EventTypes.OrderRefunded,
            EventTypes.UserRemoved
        };
    }

    protected override async Task HandleEventAsync(string eventType, string payload, IServiceScope scope, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case EventTypes.BookPublished:
                var publishedEvent = DeserializeEvent<BookPublishedV1>(payload);
                if (publishedEvent != null)
                {
                    var publishedConsumer = scope.ServiceProvider.GetRequiredService<BookPublishedConsumer>();
                    await publishedConsumer.HandleAsync(publishedEvent, cancellationToken);
                }
                break;

            case EventTypes.BookUpdated:
                var updatedEvent = DeserializeEvent<BookUpdatedV1>(payload);
                if (updatedEvent != null)
                {
                    var updatedConsumer = scope.ServiceProvider.GetRequiredService<BookUpdatedConsumer>();
                    await updatedConsumer.HandleAsync(updatedEvent, cancellationToken);
                }
                break;

            case EventTypes.BookRemoved:
                var removedEvent = DeserializeEvent<BookRemovedV1>(payload);
                if (removedEvent != null)
                {
                    var removedConsumer = scope.ServiceProvider.GetRequiredService<BookRemovedConsumer>();
                    await removedConsumer.HandleAsync(removedEvent, cancellationToken);
                }
                break;

            case EventTypes.OrderPaid:
                var orderPaidEvent = DeserializeEvent<OrderPaidV1>(payload);
                if (orderPaidEvent != null)
                {
                    var orderPaidConsumer = scope.ServiceProvider.GetRequiredService<OrderPaidConsumer>();
                    await orderPaidConsumer.HandleAsync(orderPaidEvent, cancellationToken);
                }
                break;

            case EventTypes.OrderRefunded:
                var orderRefundedEvent = DeserializeEvent<OrderRefundedV1>(payload);
                if (orderRefundedEvent != null)
                {
                    var orderRefundedConsumer = scope.ServiceProvider.GetRequiredService<OrderRefundedConsumer>();
                    await orderRefundedConsumer.HandleAsync(orderRefundedEvent, cancellationToken);
                }
                break;

            case EventTypes.UserRemoved:
                var userRemovedEvent = DeserializeEvent<UserRemovedV1>(payload);
                if (userRemovedEvent != null)
                {
                    var userRemovedConsumer = scope.ServiceProvider.GetRequiredService<UserRemovedConsumer>();
                    await userRemovedConsumer.HandleAsync(userRemovedEvent, cancellationToken);
                }
                break;
        }
    }
}

