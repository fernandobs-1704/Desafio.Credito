using Azure.Messaging.ServiceBus;
using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;
using Desafio.Credito.Worker.Clients;
using Desafio.Credito.Worker.Enums;
using Desafio.Credito.Worker.Logging;
using Desafio.Credito.Worker.Services;
using Newtonsoft.Json;

namespace Desafio.Credito.Worker.Consumers;

public class ServiceBusConsumer
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private ServiceBusProcessor _processor;
    //private int _mensagensProcessadas = 0;

    public ServiceBusConsumer(IConfiguration config, IServiceProvider serviceProvider)
    {
        _config = config;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = _config["ServiceBus:ConnectionString"];
        var queueName = _config["ServiceBus:QueueName"];

        var client = new ServiceBusClient(connectionString);

        _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            PrefetchCount = 0
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested &&
               _processor != null &&
               _processor.IsProcessing)
        {
            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        // Opção para ler somente uma única mensagem para os testes pois a fila está com muitas msgs
        //if (_mensagensProcessadas >= 1)
        //{
        //    return;
        //}

        string body = args.Message.Body.ToString();
        string queueName = _config["ServiceBus:QueueName"] ?? string.Empty;
        string messageId = args.Message.MessageId ?? string.Empty;
        string correlationId = args.Message.CorrelationId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        var resultado = await ProcessarMensagemCoreAsync(
            body,
            queueName,
            messageId,
            correlationId,
            args.Message.DeliveryCount,
            args.Message.EnqueuedTime);

        if (resultado == ResultadoProcessamentoMensagem.Complete)
        {
            await args.CompleteMessageAsync(args.Message);
        }
        else if (resultado == ResultadoProcessamentoMensagem.DeadLetter)
        {
            await args.DeadLetterMessageAsync(args.Message, "Payload inválido");
        }
        else
        {
            await args.AbandonMessageAsync(args.Message);
        }

        //_mensagensProcessadas = 1;
        await _processor.StopProcessingAsync();
    }

    public async Task<ResultadoProcessamentoMensagem> ProcessarMensagemCoreAsync(
        string body,
        string queueName,
        string messageId,
        string correlationId,
        int deliveryCount,
        DateTimeOffset enqueuedTimeUtc)
    {
        using var scope = _serviceProvider.CreateScope();

        var apiClient = scope.ServiceProvider.GetRequiredService<IPriceApiClient>();
        var repository = scope.ServiceProvider.GetRequiredService<IEvolucaoContratoRepository>();
        var logService = scope.ServiceProvider.GetRequiredService<ILogEventService>();

        string detalheInicio =
            "Mensagem recebida da fila para processamento. " +
            "DeliveryCount: " + deliveryCount +
            " | EnqueuedTimeUtc: " + enqueuedTimeUtc.UtcDateTime.ToString("o");

        await TentarRegistrarLogAsync(
            logService,
            CriarLog(
                LogAcao.InicioProcessamento,
                LogStatus.Iniciado,
                messageId,
                correlationId,
                queueName,
                body,
                detalheInicio,
                null));

        try
        {
            var request = JsonConvert.DeserializeObject<CalcularPriceRequestDto>(body);

            if (request == null ||
                request.ValorEmprestimo <= 0 ||
                request.TaxaJurosMensal <= 0 ||
                request.PrazoMeses <= 0)
            {
                await TentarRegistrarLogAsync(
                    logService,
                    CriarLog(
                        LogAcao.ValidacaoMensagem,
                        LogStatus.ErroValidacao,
                        messageId,
                        correlationId,
                        queueName,
                        body,
                        "Payload inválido. Os campos valorEmprestimo, taxaJurosMensal e prazoMeses devem ser maiores que zero.",
                        null));

                await TentarRegistrarLogAsync(
                    logService,
                    CriarLog(
                        LogAcao.DeadLetter,
                        LogStatus.DeadLetter,
                        messageId,
                        correlationId,
                        queueName,
                        body,
                        "Mensagem enviada para Dead Letter Queue por falha de validação.",
                        null));

                return ResultadoProcessamentoMensagem.DeadLetter;
            }

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.ChamadaApiPrice,
                    LogStatus.Iniciado,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Iniciando chamada à API de cálculo Price.",
                    null));

            var response = await apiClient.CalcularAsync(request);

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.ChamadaApiPrice,
                    LogStatus.Sucesso,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Chamada à API Price concluída com sucesso.",
                    null));

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.PersistenciaBanco,
                    LogStatus.Iniciado,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Iniciando persistência da evolução do contrato no banco de dados.",
                    null));

            await repository.SalvarAsync(response, CancellationToken.None);

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.PersistenciaBanco,
                    LogStatus.Sucesso,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Persistência da evolução do contrato concluída com sucesso.",
                    null));

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.FimProcessamento,
                    LogStatus.Sucesso,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Mensagem processada com sucesso e concluída no Service Bus.",
                    null));

            return ResultadoProcessamentoMensagem.Complete;
        }
        catch (HttpRequestException ex)
        {
            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.ChamadaApiPrice,
                    LogStatus.ErroApi,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Erro ao chamar a API Price.",
                    ex));

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.AbandonoMensagem,
                    LogStatus.Abandonado,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Mensagem abandonada após erro na chamada da API.",
                    null));

            return ResultadoProcessamentoMensagem.Abandon;
        }
        catch (InvalidOperationException ex)
        {
            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.PersistenciaBanco,
                    LogStatus.ErroPersistencia,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Erro operacional durante o processamento ou persistência dos dados.",
                    ex));

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.AbandonoMensagem,
                    LogStatus.Abandonado,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Mensagem abandonada após erro operacional.",
                    null));

            return ResultadoProcessamentoMensagem.Abandon;
        }
        catch (Exception ex)
        {
            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.ErroInfraestrutura,
                    LogStatus.ErroServiceBus,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Erro não tratado durante o processamento da mensagem.",
                    ex));

            await TentarRegistrarLogAsync(
                logService,
                CriarLog(
                    LogAcao.AbandonoMensagem,
                    LogStatus.Abandonado,
                    messageId,
                    correlationId,
                    queueName,
                    body,
                    "Mensagem abandonada após erro inesperado.",
                    null));

            return ResultadoProcessamentoMensagem.Abandon;
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("ERRO NO SERVICE BUS");
        Console.WriteLine("Source: " + args.ErrorSource);
        Console.WriteLine("Entity Path: " + args.EntityPath);
        Console.WriteLine("Namespace: " + args.FullyQualifiedNamespace);
        Console.WriteLine("Mensagem: " + args.Exception.Message);
        Console.WriteLine("StackTrace: " + args.Exception.ToString());
        Console.WriteLine("==========================================");

        return Task.CompletedTask;
    }

    private static LogDto CriarLog(
        LogAcao acao,
        LogStatus status,
        string messageId,
        string correlationId,
        string queueName,
        string payload,
        string detalhe,
        Exception ex)
    {
        return new LogDto
        {
            Acao = acao,
            Status = status,
            MessageId = messageId,
            CorrelationId = correlationId,
            QueueName = queueName,
            Payload = payload,
            Detalhe = detalhe,
            Exception = ex != null ? ex.ToString() : null,
            DataHoraUtc = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            Ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };
    }

    private async Task TentarRegistrarLogAsync(ILogEventService logService, LogDto log)
    {
        try
        {
            await logService.LogAsync(log);
        }
        catch
        {
        }
    }
}