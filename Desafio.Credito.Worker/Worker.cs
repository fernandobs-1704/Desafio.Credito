using Desafio.Credito.Worker.Services;

namespace Desafio.Credito.Worker;

public class Worker : BackgroundService
{
    private readonly ServiceBusConsumer _consumer;
    private readonly ILogger<Worker> _logger;

    public Worker(ServiceBusConsumer consumer, ILogger<Worker> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado");

        await _consumer.StartAsync();

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}