namespace Desafio.Credito.Worker.Configurations;

public class ServiceBusSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}