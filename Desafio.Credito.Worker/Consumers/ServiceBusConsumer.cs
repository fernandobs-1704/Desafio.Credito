using Azure.Messaging.ServiceBus;
using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;
using Desafio.Credito.Worker.Clients;
using Desafio.Credito.Worker.Logging;
using Desafio.Credito.Worker.Services;
using Newtonsoft.Json;

namespace Desafio.Credito.Worker.Consumers;

public class ServiceBusConsumer
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private ServiceBusProcessor _processor;
    private int _mensagensProcessadas = 0;

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

        // Mantém o worker vivo até cancelarem ou até a gente parar o processor
        while (!cancellationToken.IsCancellationRequested &&
               _processor != null &&
               _processor.IsProcessing)
        {
            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        // Blindagem extra para processar só 1 mensagem
        if (_mensagensProcessadas >= 1)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();

        var apiClient = scope.ServiceProvider.GetRequiredService<PriceApiClient>();
        var repository = scope.ServiceProvider.GetRequiredService<IEvolucaoContratoRepository>();
        var logService = scope.ServiceProvider.GetRequiredService<ILogEventService>();

        string body = args.Message.Body.ToString();
        string queueName = _config["ServiceBus:QueueName"] ?? string.Empty;
        string messageId = args.Message.MessageId ?? string.Empty;
        string correlationId = args.Message.CorrelationId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        Console.WriteLine("----- MENSAGEM RECEBIDA -----");
        Console.WriteLine(body);

        string detalheInicio =
            "Mensagem recebida da fila para processamento. " +
            "DeliveryCount: " + args.Message.DeliveryCount +
            " | EnqueuedTimeUtc: " + args.Message.EnqueuedTime.UtcDateTime.ToString("o");

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
                Console.WriteLine("Payload inválido. Enviando para DeadLetter...");

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

                await args.DeadLetterMessageAsync(args.Message, "Payload inválido");

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

                Console.WriteLine("Mensagem enviada para DeadLetter.");

                _mensagensProcessadas = 1;
                await _processor.StopProcessingAsync();

                return;
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

            Console.WriteLine("Chamando API...");
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

            Console.WriteLine("Salvando no banco...");
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

            Console.WriteLine("Finalizando mensagem...");
            await args.CompleteMessageAsync(args.Message);

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

            Console.WriteLine("Mensagem processada com sucesso.");

            _mensagensProcessadas = 1;
            await _processor.StopProcessingAsync();

            Console.WriteLine("Worker parado após processar 1 única mensagem.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("######## ERRO NA CHAMADA DA API ########");
            Console.WriteLine("Mensagem: " + ex.Message);
            Console.WriteLine("Detalhes: " + ex.ToString());
            Console.WriteLine("########################################");

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

            await args.AbandonMessageAsync(args.Message);

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

            _mensagensProcessadas = 1;
            await _processor.StopProcessingAsync();

            Console.WriteLine("Worker parado após erro na 1ª mensagem.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("######## ERRO DE PROCESSAMENTO ########");
            Console.WriteLine("Mensagem: " + ex.Message);
            Console.WriteLine("Detalhes: " + ex.ToString());
            Console.WriteLine("######################################");

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

            await args.AbandonMessageAsync(args.Message);

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

            _mensagensProcessadas = 1;
            await _processor.StopProcessingAsync();

            Console.WriteLine("Worker parado após erro operacional na 1ª mensagem.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("######## ERRO NO PROCESSAMENTO ########");
            Console.WriteLine("Mensagem: " + ex.Message);
            Console.WriteLine("Detalhes: " + ex.ToString());
            Console.WriteLine("######################################");

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

            await args.AbandonMessageAsync(args.Message);

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

            _mensagensProcessadas = 1;
            await _processor.StopProcessingAsync();

            Console.WriteLine("Worker parado após erro inesperado na 1ª mensagem.");
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
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao gravar log no Event Hub: " + ex.Message);
        }
    }
}