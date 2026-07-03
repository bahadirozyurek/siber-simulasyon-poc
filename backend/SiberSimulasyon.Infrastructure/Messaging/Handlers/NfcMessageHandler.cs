using System.Text.Json;
using Microsoft.Extensions.Logging;
using SiberSimulasyon.Core.Entities;
using SiberSimulasyon.Core.Messaging;
using SiberSimulasyon.Infrastructure.Data;

namespace SiberSimulasyon.Infrastructure.Messaging.Handlers;

public class NfcMessageHandler(AppDbContext db, ILogger<NfcMessageHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task HandleAsync(string json, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<NfcEventMessage>(json, JsonOptions)
            ?? throw new InvalidOperationException("NFC mesaji okunamadi.");

        db.NfcLogs.Add(new NfcLog
        {
            PersonId = message.PersonId,
            FullName = message.FullName,
            Type = message.Type,
            Timestamp = message.Timestamp
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("NFC kaydi: {Type} - {FullName} ({PersonId})", message.Type, message.FullName, message.PersonId);
    }
}
