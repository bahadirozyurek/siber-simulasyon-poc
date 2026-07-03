namespace SiberSimulasyon.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "broker_user";
    public string Password { get; set; } = "BrokerSecurePass321!!";
}