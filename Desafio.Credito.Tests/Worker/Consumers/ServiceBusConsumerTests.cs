using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;
using Desafio.Credito.Worker.Clients;
using Desafio.Credito.Worker.Consumers;
using Desafio.Credito.Worker.Enums;
using Desafio.Credito.Worker.Logging;
using Desafio.Credito.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Desafio.Credito.Tests.Worker.Consumers;

public class ServiceBusConsumerTests
{
    private readonly Mock<IPriceApiClient> _apiClientMock;
    private readonly Mock<IEvolucaoContratoRepository> _repositoryMock;
    private readonly Mock<ILogEventService> _logServiceMock;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceBusConsumer _consumer;

    public ServiceBusConsumerTests()
    {
        _apiClientMock = new Mock<IPriceApiClient>();
        _repositoryMock = new Mock<IEvolucaoContratoRepository>();
        _logServiceMock = new Mock<ILogEventService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ServiceBus:QueueName", "fila-teste" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var services = new ServiceCollection();

        services.AddScoped(_ => _apiClientMock.Object);
        services.AddScoped(_ => _repositoryMock.Object);
        services.AddScoped(_ => _logServiceMock.Object);

        _serviceProvider = services.BuildServiceProvider();

        _consumer = new ServiceBusConsumer(_configuration, _serviceProvider);
    }

    [Fact]
    public async Task ProcessarMensagemCoreAsync_DeveRetornarComplete_QuandoPayloadForValido()
    {
        var body = """
                   {
                     "valorEmprestimo": 10000,
                     "taxaJurosMensal": 1.8,
                     "prazoMeses": 30
                   }
                   """;

        var response = new CalcularPriceResponseDto
        {
            Evolucao = new List<EvolucaoPriceDto>
            {
                new EvolucaoPriceDto
                {
                    Dia = 1,
                    Prestacao = 434.31m,
                    JurosPeriodo = 6.00m,
                    Amortizacao = 0m,
                    SaldoAposPagar = 10006.00m
                }
            }
        };

        _apiClientMock
            .Setup(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()))
            .ReturnsAsync(response);

        var resultado = await _consumer.ProcessarMensagemCoreAsync(
            body,
            "fila-teste",
            "msg-001",
            "corr-001",
            1,
            DateTimeOffset.UtcNow);

        resultado.Should().Be(ResultadoProcessamentoMensagem.Complete);

        _apiClientMock.Verify(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()), Times.Once);
        _repositoryMock.Verify(x => x.SalvarAsync(response, It.IsAny<CancellationToken>()), Times.Once);
        _logServiceMock.Verify(x => x.LogAsync(It.IsAny<LogDto>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessarMensagemCoreAsync_DeveRetornarDeadLetter_QuandoPayloadForInvalido()
    {
        var body = """
                   {
                     "valorEmprestimo": 0,
                     "taxaJurosMensal": 1.8,
                     "prazoMeses": 30
                   }
                   """;

        var resultado = await _consumer.ProcessarMensagemCoreAsync(
            body,
            "fila-teste",
            "msg-002",
            "corr-002",
            1,
            DateTimeOffset.UtcNow);

        resultado.Should().Be(ResultadoProcessamentoMensagem.DeadLetter);

        _apiClientMock.Verify(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()), Times.Never);
        _repositoryMock.Verify(x => x.SalvarAsync(It.IsAny<CalcularPriceResponseDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _logServiceMock.Verify(x => x.LogAsync(It.IsAny<LogDto>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessarMensagemCoreAsync_DeveRetornarAbandon_QuandoApiFalhar()
    {
        var body = """
                   {
                     "valorEmprestimo": 10000,
                     "taxaJurosMensal": 1.8,
                     "prazoMeses": 30
                   }
                   """;

        _apiClientMock
            .Setup(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()))
            .ThrowsAsync(new HttpRequestException("Falha na API"));

        var resultado = await _consumer.ProcessarMensagemCoreAsync(
            body,
            "fila-teste",
            "msg-003",
            "corr-003",
            1,
            DateTimeOffset.UtcNow);

        resultado.Should().Be(ResultadoProcessamentoMensagem.Abandon);

        _repositoryMock.Verify(x => x.SalvarAsync(It.IsAny<CalcularPriceResponseDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessarMensagemCoreAsync_DeveRetornarAbandon_QuandoPersistenciaFalhar()
    {
        var body = """
                   {
                     "valorEmprestimo": 10000,
                     "taxaJurosMensal": 1.8,
                     "prazoMeses": 30
                   }
                   """;

        var response = new CalcularPriceResponseDto
        {
            Evolucao = new List<EvolucaoPriceDto>
            {
                new EvolucaoPriceDto
                {
                    Dia = 1,
                    Prestacao = 434.31m,
                    JurosPeriodo = 6.00m,
                    Amortizacao = 0m,
                    SaldoAposPagar = 10006.00m
                }
            }
        };

        _apiClientMock
            .Setup(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()))
            .ReturnsAsync(response);

        _repositoryMock
            .Setup(x => x.SalvarAsync(It.IsAny<CalcularPriceResponseDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falha ao persistir"));

        var resultado = await _consumer.ProcessarMensagemCoreAsync(
            body,
            "fila-teste",
            "msg-004",
            "corr-004",
            1,
            DateTimeOffset.UtcNow);

        resultado.Should().Be(ResultadoProcessamentoMensagem.Abandon);

        _apiClientMock.Verify(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()), Times.Once);
        _repositoryMock.Verify(x => x.SalvarAsync(It.IsAny<CalcularPriceResponseDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessarMensagemCoreAsync_DeveRegistrarPayloadOriginalNoLogInicial()
    {
        var body = """
                   {
                     "valorEmprestimo": 10000,
                     "taxaJurosMensal": 1.8,
                     "prazoMeses": 30
                   }
                   """;

        var response = new CalcularPriceResponseDto
        {
            Evolucao = new List<EvolucaoPriceDto>()
        };

        _apiClientMock
            .Setup(x => x.CalcularAsync(It.IsAny<CalcularPriceRequestDto>()))
            .ReturnsAsync(response);

        LogDto? primeiroLog = null;

        _logServiceMock
            .Setup(x => x.LogAsync(It.IsAny<LogDto>()))
            .Callback<LogDto>(log =>
            {
                if (primeiroLog == null)
                {
                    primeiroLog = log;
                }
            })
            .Returns(Task.CompletedTask);

        var resultado = await _consumer.ProcessarMensagemCoreAsync(
            body,
            "fila-teste",
            "msg-005",
            "corr-005",
            1,
            DateTimeOffset.UtcNow);

        resultado.Should().Be(ResultadoProcessamentoMensagem.Complete);

        primeiroLog.Should().NotBeNull();
        primeiroLog!.Acao.Should().Be(LogAcao.InicioProcessamento);
        primeiroLog.Status.Should().Be(LogStatus.Iniciado);
        primeiroLog.Payload.Should().Be(body);
    }
}