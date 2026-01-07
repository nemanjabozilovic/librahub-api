using LibraHub.BuildingBlocks.Messaging;
using LibraHub.Catalog.Application.Consumers;
using LibraHub.Contracts.Common;
using LibraHub.Contracts.Content.V1;
using RabbitMQ.Client;

namespace LibraHub.Catalog.Api.Workers;

public class CatalogEventConsumerWorker : EventConsumerWorker
{
    public CatalogEventConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<CatalogEventConsumerWorker> logger,
        IConnection connection)
        : base(serviceProvider, logger, connection, "catalog-events", "librahub.events")
    {
    }

    protected override IEnumerable<string> GetSubscribedEventTypes()
    {
        return new[]
        {
            EventTypes.CoverUploaded,
            EventTypes.EditionUploaded
        };
    }

    protected override async Task HandleEventAsync(string eventType, string payload, IServiceScope scope, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case EventTypes.CoverUploaded:
                var coverEvent = DeserializeEvent<CoverUploadedV1>(payload);
                if (coverEvent != null)
                {
                    var coverConsumer = scope.ServiceProvider.GetRequiredService<CoverUploadedConsumer>();
                    await coverConsumer.HandleAsync(coverEvent, cancellationToken);
                }
                break;

            case EventTypes.EditionUploaded:
                var editionEvent = DeserializeEvent<EditionUploadedV1>(payload);
                if (editionEvent != null)
                {
                    var editionConsumer = scope.ServiceProvider.GetRequiredService<EditionUploadedConsumer>();
                    await editionConsumer.HandleAsync(editionEvent, cancellationToken);
                }
                break;
        }
    }
}
