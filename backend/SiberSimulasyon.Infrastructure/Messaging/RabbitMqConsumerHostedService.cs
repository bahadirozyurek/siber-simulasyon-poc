using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Messaging.Handlers;

namespace SiberSimulasyon.Infrastructure.Messaging;

public class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerHostedService(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerHostedService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ baglantisi koptu, 5 saniye sonra tekrar denenecek.");
                await CleanupAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await RabbitMqPublisher.DeclareQueuesAsync(_channel, stoppingToken);
        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        await StartConsumerAsync(QueueNames.CameraEvents, HandleCameraAsync, stoppingToken);
        await StartConsumerAsync(QueueNames.NfcEvents, HandleNfcAsync, stoppingToken);
        await StartConsumerAsync(QueueNames.SystemHeartbeat, HandleSystemAsync, stoppingToken);

        _logger.LogInformation("RabbitMQ consumer baslatildi.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task StartConsumerAsync(
        string queueName,
        Func<string, CancellationToken, Task> handler,
        CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                await handler(json, stoppingToken);
                await _channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mesaj islenemedi: {Queue}", queueName);
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
            }
        };

        await _channel!.BasicConsumeAsync(queueName, autoAck: false, consumer, stoppingToken);
    }

    private async Task HandleCameraAsync(string json, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CameraMessageHandler>();
        await handler.HandleAsync(json, cancellationToken);
    }

    private async Task HandleNfcAsync(string json, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<NfcMessageHandler>();
        await handler.HandleAsync(json, cancellationToken);
    }

    private async Task HandleSystemAsync(string json, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SystemHeartbeatHandler>();
        await handler.HandleAsync(json, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CleanupAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task CleanupAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
