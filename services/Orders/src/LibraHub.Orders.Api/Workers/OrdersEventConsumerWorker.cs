using LibraHub.BuildingBlocks.Messaging;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Orders.V1;
using LibraHub.Orders.Application.Consumers;
using RabbitMQ.Client;

namespace LibraHub.Orders.Api.Workers;

public class OrdersEventConsumerWorker : EventConsumerWorker
{
    public OrdersEventConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<OrdersEventConsumerWorker> logger,
        IConnection connection)
        : base(serviceProvider, logger, connection, "orders-events", "librahub.events")
    {
    }

    protected override IEnumerable<string> GetSubscribedEventTypes()
    {
        return new[]
        {
            EventTypes.OrderRefunded
        };
    }

    protected override async Task HandleEventAsync(string eventType, string payload, IServiceScope scope, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case EventTypes.OrderRefunded:
                var orderRefundedEvent = DeserializeEvent<OrderRefundedV1>(payload);
                if (orderRefundedEvent != null)
                {
                    var orderRefundedConsumer = scope.ServiceProvider.GetRequiredService<OrderRefundedConsumer>();
                    await orderRefundedConsumer.HandleAsync(orderRefundedEvent, cancellationToken);
                }
                break;
        }
    }
}
