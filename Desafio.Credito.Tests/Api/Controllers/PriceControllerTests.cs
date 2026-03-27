using Desafio.Credito.Api.Controllers;
using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Desafio.Credito.Tests.Api.Controllers;

public class PriceControllerTests
{
    private readonly Mock<ICalculadoraPriceService> _calculadoraPriceServiceMock;
    private readonly PriceController _controller;

    public PriceControllerTests()
    {
        _calculadoraPriceServiceMock = new Mock<ICalculadoraPriceService>();
        _controller = new PriceController(_calculadoraPriceServiceMock.Object);
    }

    [Fact]
    public void Calcular_DeveRetornarBadRequest_QuandoRequestForNulo()
    {
        CalcularPriceRequestDto request = null;

        var result = _controller.Calcular(request);

        result.Should().BeOfType<BadRequestObjectResult>();

        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("Payload não informado.");
    }

    [Fact]
    public void Calcular_DeveRetornarBadRequest_QuandoValorEmprestimoForMenorOuIgualAZero()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 0m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 30
        };

        var result = _controller.Calcular(request);

        result.Should().BeOfType<BadRequestObjectResult>();

        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("O valor do empréstimo deve ser maior que zero.");
    }

    [Fact]
    public void Calcular_DeveRetornarBadRequest_QuandoTaxaJurosMensalForMenorOuIgualAZero()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 0m,
            PrazoMeses = 30
        };

        var result = _controller.Calcular(request);

        result.Should().BeOfType<BadRequestObjectResult>();

        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("A taxa de juros mensal deve ser maior que zero.");
    }

    [Fact]
    public void Calcular_DeveRetornarBadRequest_QuandoPrazoMesesForMenorOuIgualAZero()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 0
        };

        var result = _controller.Calcular(request);

        result.Should().BeOfType<BadRequestObjectResult>();

        var badRequest = result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("O prazo em meses deve ser maior que zero.");
    }

    [Fact]
    public void Calcular_DeveRetornarOkComResultado_QuandoRequestForValido()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 30
        };

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

        _calculadoraPriceServiceMock
            .Setup(x => x.Calcular(request))
            .Returns(response);

        var result = _controller.Calcular(request);

        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(response);

        _calculadoraPriceServiceMock.Verify(x => x.Calcular(request), Times.Once);
    }

    [Fact]
    public void Calcular_NaoDeveChamarServico_QuandoRequestForInvalido()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = -1m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 30
        };

        var result = _controller.Calcular(request);

        result.Should().BeOfType<BadRequestObjectResult>();

        _calculadoraPriceServiceMock.Verify(x => x.Calcular(It.IsAny<CalcularPriceRequestDto>()), Times.Never);
    }
}