using SiberSimulasyon.Infrastructure;
using SiberSimulasyon.Worker.SystemAdmin;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRabbitMqPublisher(builder.Configuration);
builder.Services.AddHostedService<SystemAdminSimulatorWorker>();
builder.Build().Run();
