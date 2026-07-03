using System.Net;
using System.Net.Sockets;
using System.Text;
using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Messaging;

namespace SiberSimulasyon.Worker.SystemAdmin;

public class SystemAdminSimulatorWorker(
    RabbitMqPublisher publisher,
    ILogger<SystemAdminSimulatorWorker> logger) : BackgroundService
{
    private const int Port = 2222;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForBrokerAsync(stoppingToken);

        using var udpClient = new UdpClient(Port);
        logger.LogInformation("🛡️ Linux Sunucu Ajanı UDP {Port} portunu dinlemeye basladi...", Port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveResult = await udpClient.ReceiveAsync(stoppingToken);
                var messageStr = Encoding.UTF8.GetString(receiveResult.Buffer).Trim();

                var parts = messageStr.Split(':');
                if (parts.Length == 2)
                {
                    var hostname = parts[0];
                    var isActive = parts[1].Equals("AKTIF", StringComparison.OrdinalIgnoreCase);

                    var message = new SystemHeartbeatMessage
                    {
                        Hostname = hostname,
                        IsActive = isActive,
                        Timestamp = DateTime.UtcNow
                    };

                    await publisher.PublishAsync(QueueNames.SystemHeartbeat, message, stoppingToken);
                    logger.LogInformation("Sinyal Alindi -> {Hostname}: {Status}", hostname, isActive ? "Aktif" : "Pasif");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "UDP Sinyal okuma hatasi.");
            }
        }
    }

    private async Task WaitForBrokerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await publisher.EnsureConnectedAsync(stoppingToken); return; }
            catch { await Task.Delay(5000, stoppingToken); }
        }
    }
}