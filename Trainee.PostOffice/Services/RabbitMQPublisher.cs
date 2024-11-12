using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using Trainee.PostOffice.Configuration;

namespace Trainee.PostOffice.Services;

public class RabbitMQPublisher(ILogger<RabbitMQPublisher> logger, IOptions<RabbitMQConfiguration> config) : IDisposable
{
    private readonly ILogger<RabbitMQPublisher> _logger = logger;
    private readonly RabbitMQConfiguration _config = config.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task Publish(string queueDestionation, string message)
    {
        if (_connection is null) throw new Exception("Cannot publish, connection not set");

        try
        {
            _semaphore.Wait();
            if (_channel is null)
            {
                _channel = await _connection.CreateChannelAsync();
                await _channel.QueueDeclareAsync(queueDestionation,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?> { { "x-queue-type", "classic" } });
            }

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueDestionation,
                mandatory: false,
                body: Encoding.UTF8.GetBytes(message));

            _logger.LogTrace("Successfully sent to: '{queue}' message: {body}", queueDestionation, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed publish");
        }
        finally
        {
            _semaphore.Release();
        }
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
        _channel = await _connection.CreateChannelAsync();
    }

    public void Dispose()
    {
        try
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while closing the connection.");
        }
    }
}
