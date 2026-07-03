using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiberSimulasyon.Infrastructure.Data;
using SiberSimulasyon.Infrastructure.Messaging;
using SiberSimulasyon.Infrastructure.Messaging.Handlers;

namespace SiberSimulasyon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqPublisher>();
        services.AddScoped<CameraMessageHandler>();
        services.AddScoped<NfcMessageHandler>();
        services.AddScoped<SystemHeartbeatHandler>();
        
        // Hosted Servisi doğrudan altyapı katmanında güvenli şekilde bağlıyoruz
        services.AddHostedService<RabbitMqConsumerHostedService>();

        return services;
    }

    public static IServiceCollection AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqPublisher>();
        return services;
    }
}
