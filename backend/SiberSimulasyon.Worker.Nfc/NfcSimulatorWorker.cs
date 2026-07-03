using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Messaging;

namespace SiberSimulasyon.Worker.Nfc;

public class NfcSimulatorWorker(
    RabbitMqPublisher publisher,
    ILogger<NfcSimulatorWorker> logger) : BackgroundService
{
    private const string FilePath = "/app/sensors/nfc_pipe.txt";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForBrokerAsync(stoppingToken);

        var dir = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        if (!File.Exists(FilePath)) await File.WriteAllTextAsync(FilePath, "", stoppingToken);

        logger.LogInformation("📟 NFC Okuyucu Simulatörü '{FilePath}' dosyasini izliyor...", FilePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(FilePath, stoppingToken);
                if (lines.Length > 0)
                {
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split(',');
                        if (parts.Length == 3)
                        {
                            var message = new NfcEventMessage
                            {
                                PersonId = parts[0].Trim(),
                                FullName = parts[1].Trim(),
                                Type = parts[2].Trim().ToUpper(),
                                Timestamp = DateTime.UtcNow
                            };

                            await publisher.PublishAsync(QueueNames.NfcEvents, message, stoppingToken);
                            logger.LogInformation("NFC Kart Tetiklendi: {Name} -> {Type}", message.FullName, message.Type);
                        }
                    }
                    // Okunan satırları temizle ki tekrar işlemesin
                    await File.WriteAllTextAsync(FilePath, "", stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "NFC dosya okuma hatasi.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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