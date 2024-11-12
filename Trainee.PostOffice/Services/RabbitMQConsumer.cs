using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;
using Trainee.PostOffice.Configuration;

namespace Trainee.PostOffice.Services;

public class RabbitMQConsumer(ILogger<RabbitMQConsumer> logger, IOptions<RabbitMQConfiguration> config) : IDisposable
{
    private readonly ILogger<RabbitMQConsumer> _logger = logger;
    private readonly RabbitMQConfiguration _config = config.Value;
    private IConnection? _connection;

    public async Task Consume(string fromQueue, Func<string, Task> onRecieved)
    {
        if (_connection is null)
        {
            _logger.LogError("Cannot receive from: '{queue}' because rabbitmq is not connected", fromQueue);
            return;
        }

        var channel = await _connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            queue: fromQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { { "x-queue-type", "classic" } });

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (ch, ea) =>
        {
            string message = string.Empty;
            try
            {
                message = Encoding.UTF8.GetString(ea.Body.ToArray());
                await onRecieved(message);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogTrace("Successfully received from: '{queue}' message: {body}", fromQueue, message);
            }
            catch (Exception ex)
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                _logger.LogError(ex, "Cannot receive from: '{queue}' message: '{body}'", fromQueue, message);
            }
        };
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 2, global: false);
        await channel.BasicConsumeAsync(fromQueue, autoAck: false, consumer);
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_config.HostNames))
        {
            _logger.LogWarning("Empty HostNames parameter, cannot create connection to RabbitMQ.");
            throw new InvalidOperationException("HostNames parameter is required for RabbitMQ connection.");
        }

        var factory = new ConnectionFactory
        {
            ClientProvidedName = _config.ClientProviderName,
            UserName = _config.UserName ?? ConnectionFactory.DefaultUser,
            Password = _config.Password ?? ConnectionFactory.DefaultPass,
            VirtualHost = _config.VirtualHost ?? ConnectionFactory.DefaultVHost,
            Port = int.Parse(_config.Port ?? "5672"),
            AutomaticRecoveryEnabled = true,
            ConsumerDispatchConcurrency = 1
        };
        var endpoints = _config.HostNames
            .Split(' ', ',', ';')
            .Select(hostname => new AmqpTcpEndpoint(hostname))
            .ToList();

        _connection = await factory.CreateConnectionAsync(endpoints);
    }

    public void Dispose()
    {
        try
        {
            _connection?.CloseAsync().GetAwaiter().GetResult();
        }
        catch (ChannelClosedException ex)
        {
            _logger.LogWarning(ex, "Connection is already closed");
        }
    }
}
