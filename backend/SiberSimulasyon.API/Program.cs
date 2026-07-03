using Microsoft.EntityFrameworkCore;
using SiberSimulasyon.Core.Entities;
using SiberSimulasyon.Infrastructure;
using SiberSimulasyon.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();

app.UseMiddleware<SiberSimulasyon.API.Middlewares.AdminActionLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.CameraFeeds.AnyAsync())
        {
            db.CameraFeeds.AddRange(
                new CameraFeed { CameraId = "cam-001", Name = "Giris Kamerasi", IsActive = false },
                new CameraFeed { CameraId = "cam-002", Name = "Otopark Kamerasi", IsActive = false },
                new CameraFeed { CameraId = "cam-003", Name = "Koridor Kamerasi", IsActive = false },
                new CameraFeed { CameraId = "cam-004", Name = "Arka Bahce Kamerasi", IsActive = false },
                new CameraFeed { CameraId = "cam-005", Name = "Depo Kamerasi", IsActive = false }
            );
        }

        if (!await db.SystemNodes.AnyAsync())
        {
            db.SystemNodes.AddRange(
                new SystemNode { Hostname = "linux-sunucu-01", IsActive = false },
                new SystemNode { Hostname = "linux-sunucu-02", IsActive = false },
                new SystemNode { Hostname = "linux-workstation-03", IsActive = false },
                new SystemNode { Hostname = "linux-db-04", IsActive = false }
            );
        }

        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new User 
            { 
                Username = "bahi_admin", 
                PasswordHash = HashPassword("AdminPass123!"), 
                Role = "ADMIN" 
            });
        }

        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "⚠️ Veritabanı ilklendirme aşamasında kritik hata!");
    }
}

// endpoint

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "siber-simulasyon-api" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapGet("/api/cameras", async (AppDbContext db) =>
    await db.CameraFeeds.OrderBy(x => x.Name).ToListAsync())
    .WithName("GetCameras")
    .WithOpenApi();

app.MapGet("/api/nfc-logs", async (AppDbContext db) =>
    await db.NfcLogs.OrderByDescending(x => x.Timestamp).Take(50).ToListAsync())
    .WithName("GetNfcLogs")
    .WithOpenApi();

app.MapGet("/api/nfc/inside-count", async (AppDbContext db) =>
{
    var logs = await db.NfcLogs.OrderBy(x => x.Timestamp).ToListAsync();
    var inside = new HashSet<string>();
    foreach (var log in logs)
    {
        var logType = log.Type.Trim().ToUpper();
        if (logType == "GIRIS") inside.Add(log.PersonId);
        else if (logType == "CIKIS") inside.Remove(log.PersonId);
    }
    return Results.Ok(new { totalInside = inside.Count, personIds = inside.ToArray() });
})
    .WithName("GetInsideCount")
    .WithOpenApi();

app.MapGet("/api/system-nodes", async (AppDbContext db) =>
    await db.SystemNodes.OrderBy(x => x.Hostname).ToListAsync())
    .WithName("GetSystemNodes")
    .WithOpenApi();

app.MapGet("/api/admin-logs", async (AppDbContext db) =>
    await db.AdminActionLogs.OrderByDescending(x => x.Timestamp).Take(50).ToListAsync())
    .WithName("GetAdminLogs")
    .WithOpenApi();

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db) =>
{
    var hashedPassword = HashPassword(request.Password);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.PasswordHash == hashedPassword);
    if (user is null) return Results.Json(new { message = "Kullanici adi veya sifre hatali!" }, statusCode: 401);
    return Results.Ok(new { username = user.Username, role = user.Role });
})
.WithName("Login")
.WithOpenApi();

app.MapPost("/api/auth/register-yetkili", async (RegisterRequest request, HttpContext context, AppDbContext db) =>
{
    context.Request.Headers.TryGetValue("X-User-Role", out var callerRole);
    if (!callerRole.ToString().Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new { message = "⚠️ GÜVENLIK IHLALI: Yetkiniz yok!" }, statusCode: 403);
    }

    if (await db.Users.AnyAsync(u => u.Username == request.Username))
        return Results.BadRequest(new { message = "Bu kullanici adi zaten alinmis!" });

    var newUser = new User { Username = request.Username, PasswordHash = HashPassword(request.Password), Role = "YETKILI" };
    db.Users.Add(newUser);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = $"'{request.Username}' isimli yeni YETKILI hesabı başarıyla oluşturuldu." });
})
.WithName("RegisterYetkili")
.WithOpenApi();

app.Run();

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password);

public partial class Program
{
    public static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
