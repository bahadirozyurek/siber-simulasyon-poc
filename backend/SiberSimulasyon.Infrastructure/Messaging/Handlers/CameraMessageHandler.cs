using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiberSimulasyon.Core.Entities;
using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Data;

namespace SiberSimulasyon.Infrastructure.Messaging.Handlers;

public class CameraMessageHandler(AppDbContext db, ILogger<CameraMessageHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleAsync(string json, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<CameraStatusMessage>(json, JsonOptions)
            ?? throw new InvalidOperationException("Kamera mesaji okunamadi.");

        var camera = await db.CameraFeeds
            .FirstOrDefaultAsync(x => x.CameraId == message.CameraId, cancellationToken);

        if (camera is null)
        {
            camera = new CameraFeed { CameraId = message.CameraId };
            db.CameraFeeds.Add(camera);
        }

        camera.Name = message.Name;
        camera.IsActive = message.IsActive;
        camera.ImageUrl = message.IsActive ? message.ImageUrl : null;
        camera.LastUpdated = message.Timestamp;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Kamera guncellendi: {CameraId} -> {Status}",
            message.CameraId,
            message.IsActive ? "Aktif" : "Pasif");
    }
}
