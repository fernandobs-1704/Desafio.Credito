using Desafio.Credito.Domain.Services;
using Desafio.Credito.Shared.Dtos.Price;
using FluentAssertions;

namespace Desafio.Credito.Tests.Domain.Services;

public class CalculadoraPriceServiceTests
{
    private readonly CalculadoraPriceService _service;

    public CalculadoraPriceServiceTests()
    {
        _service = new CalculadoraPriceService();
    }

    [Fact]
    public void Calcular_DeveGerar_900Registros_QuandoPrazoFor30Meses()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 30
        };

        var result = _service.Calcular(request);

        result.Should().NotBeNull();
        result.Evolucao.Should().NotBeNull();
        result.Evolucao.Should().HaveCount(900);
    }

    [Fact]
    public void Calcular_DeveGerar_480Registros_QuandoPrazoFor16Meses()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 65758m,
            TaxaJurosMensal = 1.04m,
            PrazoMeses = 16
        };

        var result = _service.Calcular(request);

        result.Evolucao.Should().HaveCount(480);
    }

    [Fact]
    public void Calcular_DeveManter_PrestacaoFixa_NasParcelasExcetoPossivelAjusteFinal()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 30
        };

        var result = _service.Calcular(request);

        var prestacoesDePagamento = result.Evolucao
            .Where(x => x.Dia % 30 == 0)
            .Select(x => x.Prestacao)
            .ToList();

        var prestacaoPadrao = prestacoesDePagamento.First();

        for (int i = 0; i < prestacoesDePagamento.Count - 1; i++)
        {
            prestacoesDePagamento[i].Should().Be(prestacaoPadrao);
        }

        prestacoesDePagamento.Last().Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calcular_DeveTer_Amortizacao_SomenteNosDiasMultiplosDe30()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 3
        };

        var result = _service.Calcular(request);

        foreach (var item in result.Evolucao)
        {
            if (item.Dia % 30 == 0)
            {
                item.Amortizacao.Should().BeGreaterThan(0);
            }
            else
            {
                item.Amortizacao.Should().Be(0);
            }
        }
    }

    [Fact]
    public void Calcular_DeveZerar_SaldoNoUltimoDia()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 30
        };

        var result = _service.Calcular(request);

        var ultimo = result.Evolucao.Last();

        ultimo.SaldoAposPagar.Should().Be(0);
    }

    [Fact]
    public void Calcular_DeveGerar_DiasSequenciais()
    {
        var request = new CalcularPriceRequestDto
        {
            ValorEmprestimo = 10000m,
            TaxaJurosMensal = 1.8m,
            PrazoMeses = 2
        };

        var result = _service.Calcular(request);

        for (int i = 0; i < result.Evolucao.Count; i++)
        {
            result.Evolucao[i].Dia.Should().Be(i + 1);
        }
    }
}