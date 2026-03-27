using Desafio.Credito.Worker.Consumers;
using Desafio.Credito.Worker.Services;

namespace Desafio.Credito.Worker;

public class Worker : BackgroundService
{
    private readonly ServiceBusConsumer _consumer;

    public Worker(ServiceBusConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.StartAsync(stoppingToken);
    }
}