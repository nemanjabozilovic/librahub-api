using LibraHub.BuildingBlocks.Messaging;
using LibraHub.Contracts.Catalog.V1;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Identity.V1;
using LibraHub.Contracts.Library.V1;
using LibraHub.Contracts.Orders.V1;
using RabbitMQ.Client;

namespace LibraHub.Notifications.Api.Workers;

public class NotificationsEventConsumerWorker : EventConsumerWorker
{
    public NotificationsEventConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<NotificationsEventConsumerWorker> logger,
        IConnection connection)
        : base(serviceProvider, logger, connection, "notifications-events", "librahub.events")
    {
    }

    protected override IEnumerable<string> GetSubscribedEventTypes()
    {
        return new[]
        {
            EventTypes.BookPublished,
            EventTypes.AnnouncementPublished,
            EventTypes.OrderPaid,
            EventTypes.OrderRefunded,
            EventTypes.EntitlementGranted,
            EventTypes.UserRemoved
        };
    }

    protected override async Task HandleEventAsync(string eventType, string payload, IServiceScope scope, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case EventTypes.BookPublished:
                var bookPublishedEvent = DeserializeEvent<BookPublishedV1>(payload);
                if (bookPublishedEvent != null)
                {
                    var bookPublishedConsumer = scope.ServiceProvider.GetRequiredService<Application.Consumers.BookPublishedConsumer>();
                    await bookPublishedConsumer.HandleAsync(bookPublishedEvent, cancellationToken);
                }
                break;

            case EventTypes.AnnouncementPublished:
                var announcementEvent = DeserializeEvent<AnnouncementPublishedV1>(payload);
                if (announcementEvent != null)
                {
                    var announcementConsumer = scope.ServiceProvider.GetRequiredService<Application.Consumers.AnnouncementPublishedConsumer>();
                    await announcementConsumer.HandleAsync(announcementEvent, cancellationToken);
                }
                break;

            case EventTypes.OrderPaid:
                var orderPaidEvent = DeserializeEvent<OrderPaidV1>(payload);
                if (orderPaidEvent != null)
                {
                    var orderPaidConsumer = scope.ServiceProvider.GetRequiredService<Application.Consumers.OrderPaidConsumer>();
                    await orderPaidConsumer.HandleAsync(orderPaidEvent, cancellationToken);
                }
                break;

            case EventTypes.OrderRefunded:
                var orderRefundedEvent = DeserializeEvent<OrderRefundedV1>(payload);
                if (orderRefundedEvent != null)
                {
                    var orderRefundedConsumer = scope.ServiceProvider.GetRequiredService<Application.Consumers.OrderRefundedConsumer>();
                    await orderRefundedConsumer.HandleAsync(orderRefundedEvent, cancellationToken);
                }
                break;

            case EventTypes.EntitlementGranted:
                var entitlementEvent = DeserializeEvent<EntitlementGrantedV1>(payload);
                if (entitlementEvent != null)
                {
                    var entitlementConsumer = scope.ServiceProvider.GetRequiredService<Application.Consumers.EntitlementGrantedConsumer>();
                    await entitlementConsumer.HandleAsync(entitlementEvent, cancellationToken);
                }
                break;

            case EventTypes.UserRemoved:
                var userRemovedEvent = DeserializeEvent<UserRemovedV1>(payload);
                if (userRemovedEvent != null)
                {
                    var userRemovedConsumer = scope.ServiceProvider.GetRequiredService<Application.Consumers.UserRemovedConsumer>();
                    await userRemovedConsumer.HandleAsync(userRemovedEvent, cancellationToken);
                }
                break;
        }
    }
}

