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
}
