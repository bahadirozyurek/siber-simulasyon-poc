using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiberSimulasyon.Core.Entities;
using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Data;

namespace SiberSimulasyon.Infrastructure.Messaging.Handlers;

public class SystemHeartbeatHandler(AppDbContext db, ILogger<SystemHeartbeatHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleAsync(string json, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<SystemHeartbeatMessage>(json, JsonOptions)
            ?? throw new InvalidOperationException("Heartbeat mesaji okunamadi.");

        var node = await db.SystemNodes
            .FirstOrDefaultAsync(x => x.Hostname == message.Hostname, cancellationToken);

        if (node is null)
        {
            node = new SystemNode { Hostname = message.Hostname };
            db.SystemNodes.Add(node);
        }

        node.IsActive = message.IsActive;
        node.LastHeartbeat = message.Timestamp;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Sistem heartbeat: {Hostname} -> {Status}",
            message.Hostname,
            message.IsActive ? "Aktif" : "Pasif");
    }
}
