using SiberSimulasyon.Infrastructure;
using SiberSimulasyon.Worker.Camera;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRabbitMqPublisher(builder.Configuration);
builder.Services.AddHostedService<CameraSimulatorWorker>();
builder.Build().Run();
