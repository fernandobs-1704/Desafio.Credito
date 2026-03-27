using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Desafio.Credito.Worker.Logging;
using Desafio.Credito.Worker.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace Desafio.Credito.Worker.Services;

public class LogEventService : ILogEventService
{
    private readonly IConfiguration _config;
    private readonly ILogger<LogEventService> _logger;

    public LogEventService(IConfiguration config, ILogger<LogEventService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task LogAsync(LogDto log)
    {
        var connectionString = _config["EventHub:ConnectionString"];
        var hubName = _config["EventHub:HubName"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("EventHub:ConnectionString não configurado.");
        }

        if (string.IsNullOrWhiteSpace(hubName))
        {
            throw new InvalidOperationException("EventHub:HubName não configurado.");
        }

        if (log == null)
        {
            throw new ArgumentNullException("log");
        }

        if (log.DataHoraUtc == DateTime.MinValue)
        {
            log.DataHoraUtc = DateTime.UtcNow;
        }

        if (string.IsNullOrWhiteSpace(log.MachineName))
        {
            log.MachineName = Environment.MachineName;
        }

        var json = JsonConvert.SerializeObject(
            log,
            Formatting.None,
            new StringEnumConverter());

        await using var producer = new EventHubProducerClient(connectionString, hubName);

        using EventDataBatch batch = await producer.CreateBatchAsync();

        if (!batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json))))
        {
            throw new InvalidOperationException("Não foi possível adicionar o log ao batch do Event Hub.");
        }

        await producer.SendAsync(batch);

        _logger.LogInformation(
            "Log enviado ao Event Hub. Acao: {Acao} | Status: {Status} | MessageId: {MessageId}",
            log.Acao,
            log.Status,
            log.MessageId);
    }
}