using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Domain.Services;

public class CalculadoraPriceService : ICalculadoraPriceService
{
    private const int DiasPorMes = 30;

    public CalcularPriceResponseDto Calcular(CalcularPriceRequestDto request)
    {
        var response = new CalcularPriceResponseDto();

        decimal valorEmprestimo = ArredondarMoeda(request.ValorEmprestimo);
        decimal taxaMensalPercentual = request.TaxaJurosMensal;
        int prazoMeses = request.PrazoMeses;

        int prazoDias = prazoMeses * DiasPorMes;
        decimal taxaMensal = taxaMensalPercentual / 100m;

        decimal prestacaoFixa = CalcularPrestacaoPrice(valorEmprestimo, taxaMensal, prazoMeses);
        decimal taxaDiaria = CalcularTaxaDiariaEquivalente(taxaMensal);

        decimal saldoBasePeriodo = valorEmprestimo;
        decimal saldoProjetado = valorEmprestimo;
        decimal jurosAcumuladoPeriodo = 0m;

        for (int dia = 1; dia <= prazoDias; dia++)
        {
            decimal jurosDoDia = ArredondarMoeda(saldoProjetado * taxaDiaria);
            jurosAcumuladoPeriodo = ArredondarMoeda(jurosAcumuladoPeriodo + jurosDoDia);
            saldoProjetado = ArredondarMoeda(saldoProjetado + jurosDoDia);

            decimal prestacaoDia = prestacaoFixa;
            decimal jurosPeriodoDia = jurosAcumuladoPeriodo;
            decimal amortizacaoDia = 0m;
            decimal saldoAposPagarDia = saldoProjetado;

            if (dia % DiasPorMes == 0)
            {
                amortizacaoDia = ArredondarMoeda(prestacaoFixa - jurosAcumuladoPeriodo);

                if (dia == prazoDias)
                {
                    amortizacaoDia = saldoBasePeriodo;
                    prestacaoDia = ArredondarMoeda(jurosAcumuladoPeriodo + amortizacaoDia);
                }

                saldoBasePeriodo = ArredondarMoeda(saldoBasePeriodo - amortizacaoDia);

                if (saldoBasePeriodo < 0m)
                {
                    saldoBasePeriodo = 0m;
                }

                saldoProjetado = saldoBasePeriodo;
                saldoAposPagarDia = saldoBasePeriodo;
                jurosAcumuladoPeriodo = 0m;
            }

            response.Evolucao.Add(new EvolucaoPriceDto
            {
                Dia = dia,
                Prestacao = prestacaoDia,
                JurosPeriodo = jurosPeriodoDia,
                Amortizacao = amortizacaoDia,
                SaldoAposPagar = saldoAposPagarDia
            });
        }

        return response;
    }

    private static decimal CalcularPrestacaoPrice(decimal principal, decimal taxaMensal, int prazoMeses)
    {
        if (taxaMensal == 0m)
        {
            return ArredondarMoeda(principal / prazoMeses);
        }

        double p = (double)principal;
        double i = (double)taxaMensal;
        int n = prazoMeses;

        double prestacao = p * (i * Math.Pow(1d + i, n)) / (Math.Pow(1d + i, n) - 1d);

        return ArredondarMoeda((decimal)prestacao);
    }

    private static decimal CalcularTaxaDiariaEquivalente(decimal taxaMensal)
    {
        double taxaDiaria = Math.Pow((double)(1m + taxaMensal), 1d / DiasPorMes) - 1d;
        return (decimal)taxaDiaria;
    }

    private static decimal ArredondarMoeda(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }
}