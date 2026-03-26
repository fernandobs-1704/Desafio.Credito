using Azure.Messaging.ServiceBus;
using Desafio.Credito.Worker.Configurations;
using Microsoft.Extensions.Options;

namespace Desafio.Credito.Worker.Services;

public class ServiceBusConsumer
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<ServiceBusConsumer> _logger;

    public ServiceBusConsumer(IOptions<ServiceBusSettings> settings, ILogger<ServiceBusConsumer> logger)
    {
        _logger = logger;

        var client = new ServiceBusClient(settings.Value.ConnectionString);

        _processor = client.CreateProcessor(settings.Value.QueueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false
        });
    }

    public async Task StartAsync()
    {
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync();
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();

        _logger.LogInformation("Mensagem recebida!");
        _logger.LogInformation("MessageId: {MessageId}", args.Message.MessageId);
        _logger.LogInformation("Corpo: {Mensagem}", body);

        // por enquanto só testa leitura
        await args.CompleteMessageAsync(args.Message);

        _logger.LogInformation("Mensagem finalizada com sucesso.");
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Erro no Service Bus");
        return Task.CompletedTask;
    }
}