using System.Text;
using Microsoft.EntityFrameworkCore;
using SiberSimulasyon.Core.Entities;
using SiberSimulasyon.Infrastructure.Data;

namespace SiberSimulasyon.API.Middlewares;

public class AdminActionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminActionLoggingMiddleware> _logger;

    public AdminActionLoggingMiddleware(RequestDelegate next, ILogger<AdminActionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? "";

        // GÜVENLİK DUVARI: Login ve Health endpoint'lerini loglama mekanizmasından muaf tut (Bypass)
        if (path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase) || 
            path.Contains("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        string username = "Anonymous";
        if (context.Request.Headers.TryGetValue("X-Admin-User", out var adminUser) && !string.IsNullOrEmpty(adminUser))
        {
            username = adminUser.ToString();
        }

        string role = "GUEST";
        if (context.Request.Headers.TryGetValue("X-User-Role", out var userRole) && !string.IsNullOrEmpty(userRole))
        {
            role = userRole.ToString();
        }

        try 
        {
            if (await dbContext.Database.CanConnectAsync())
            {
                var realUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (realUser != null) role = realUser.Role;
            }
        }
        catch { /* DB ilklendirme pürüzlerini bypass et */ }

        var method = context.Request.Method;

        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) && role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering();
            string payload = string.Empty;

            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                payload = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            context.Request.Query.TryGetValue("targetUser", out var targetUserVal);
            string targetUser = targetUserVal.ToString() ?? "System/All";

            var log = new AdminActionLog
            {
                AdminUsername = username,
                Action = $"Accessed or Modified: {path}",
                Method = method,
                Payload = string.IsNullOrEmpty(payload) ? "No Payload" : payload,
                TargetUser = targetUser,
                Timestamp = DateTime.UtcNow
            };
            dbContext.AdminActionLogs.Add(log);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("🛡️ [AUDIT LOG] Admin '{Admin}' performed '{Method}' on '{Path}'. Target: '{Target}'", 
                username, method, path, targetUser);
        }

        await _next(context);
    }
}
