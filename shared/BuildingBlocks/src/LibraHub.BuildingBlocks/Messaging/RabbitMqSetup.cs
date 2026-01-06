using RabbitMQ.Client;

namespace LibraHub.BuildingBlocks.Messaging;

public static class RabbitMqSetup
{
    public static IConnection CreateConnection(string connectionString)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection();
    }

    public static void ConfigureDeadLetterQueue(IModel channel, string queueName, string dlqName)
    {
        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "" },
            { "x-dead-letter-routing-key", dlqName }
        };

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments);

        channel.QueueDeclare(
            queue: dlqName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }
}
