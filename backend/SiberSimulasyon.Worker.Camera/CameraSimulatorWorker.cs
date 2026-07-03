using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Messaging;

namespace SiberSimulasyon.Worker.Camera;

public class CameraSimulatorWorker(
    RabbitMqPublisher publisher,
    ILogger<CameraSimulatorWorker> logger) : BackgroundService
{
    // mecbur dummy veri
    private readonly (string Id, string Name)[] _cameras =
    [
        ("cam-001", "Giris Kamerasi"),
        ("cam-002", "Otopark Kamerasi"),
        ("cam-003", "Koridor Kamerasi"),
        ("cam-004", "Arka Bahce Kamerasi"),
        ("cam-005", "Depo Kamerasi")
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForBrokerAsync(stoppingToken);


        // kamera içerisindeki sahte ai görünütleri ve ayarlamalır
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var camera in _cameras)
            {
                var isActive = Random.Shared.NextDouble() > 0.25;

                var message = new CameraStatusMessage
                {
                    CameraId = camera.Id,
                    Name = camera.Name,
                    IsActive = isActive,
                    ImageUrl = isActive
                        ? $"https://picsum.photos/seed/{camera.Id}/640/480"
                        : null,
                    Timestamp = DateTime.UtcNow
                };

                await publisher.PublishAsync(QueueNames.CameraEvents, message, stoppingToken);
                logger.LogInformation(
                    "{Name} ({Id}) -> {Status}",
                    camera.Name,
                    camera.Id,
                    isActive ? "Aktif" : "Pasif");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task WaitForBrokerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await publisher.EnsureConnectedAsync(stoppingToken);
                logger.LogInformation("RabbitMQ baglantisi hazir.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ bekleniyor...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
