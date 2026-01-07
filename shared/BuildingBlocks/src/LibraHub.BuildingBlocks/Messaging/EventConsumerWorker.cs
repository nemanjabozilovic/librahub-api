using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace LibraHub.BuildingBlocks.Messaging;

public abstract class EventConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventConsumerWorker> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;

    protected EventConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<EventConsumerWorker> logger,
        IConnection connection,
        string queueName,
        string exchangeName = "librahub.events")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connection = connection;
        _queueName = queueName;
        _exchangeName = exchangeName;
        _channel = _connection.CreateModel();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        SetupQueue();
    }

    private void SetupQueue()
    {
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);

        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        foreach (var routingKey in GetSubscribedEventTypes())
        {
            _channel.QueueBind(_queueName, _exchangeName, routingKey);
            _logger.LogInformation("Bound queue {QueueName} to routing key {RoutingKey}", _queueName, routingKey);
        }
    }

    protected abstract IEnumerable<string> GetSubscribedEventTypes();

    protected abstract Task HandleEventAsync(string eventType, string payload, IServiceScope scope, CancellationToken cancellationToken);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var eventType = ea.RoutingKey;
            var body = ea.Body.ToArray();
            var payload = Encoding.UTF8.GetString(body);

            _logger.LogDebug("Received event {EventType} with MessageId {MessageId}", eventType, ea.BasicProperties.MessageId);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                await HandleEventAsync(eventType, payload, scope, stoppingToken);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogDebug("Successfully processed event {EventType} with MessageId {MessageId}", eventType, ea.BasicProperties.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventType} with MessageId {MessageId}", eventType, ea.BasicProperties.MessageId);

                // Requeue the message for retry (you might want to implement dead-letter queue for repeated failures)
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Started consuming events from queue {QueueName}", _queueName);

        return Task.CompletedTask;
    }

    protected T? DeserializeEvent<T>(string payload) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize event payload");
            return null;
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
