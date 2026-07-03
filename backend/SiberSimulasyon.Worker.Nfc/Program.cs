using SiberSimulasyon.Infrastructure;
using SiberSimulasyon.Worker.Nfc;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRabbitMqPublisher(builder.Configuration);
builder.Services.AddHostedService<NfcSimulatorWorker>();
builder.Build().Run();
