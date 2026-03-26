using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Domain.Services;

public class CalculadoraPriceService : ICalculadoraPriceService
{
    public CalcularPriceResponseDto Calcular(CalcularPriceRequestDto request)
    {
        var response = new CalcularPriceResponseDto();

        decimal valorEmprestimo = request.ValorEmprestimo;
        decimal taxaMensalPercentual = request.TaxaJurosMensal;
        int prazoMeses = request.PrazoMeses;

        int prazoDias = prazoMeses * 30;

        decimal taxaMensal = taxaMensalPercentual / 100m;

        double taxaDiariaDouble = Math.Pow((double)(1 + taxaMensal), 1.0 / 30.0) - 1.0;
        decimal taxaDiaria = (decimal)taxaDiariaDouble;

        decimal prestacao = CalcularPrestacaoPrice(valorEmprestimo, taxaMensal, prazoMeses);
        decimal saldoDevedor = valorEmprestimo;
        decimal jurosAcumuladosPeriodo = 0m;

        for (int dia = 1; dia <= prazoDias; dia++)
        {
            decimal jurosDoDia = Math.Round(saldoDevedor * taxaDiaria, 10);
            jurosAcumuladosPeriodo += jurosDoDia;

            decimal prestacaoDia = 0m;
            decimal jurosPeriodoDia = 0m;
            decimal amortizacaoDia = 0m;

            if (dia % 30 == 0)
            {
                jurosPeriodoDia = Math.Round(jurosAcumuladosPeriodo, 2);
                prestacaoDia = Math.Round(prestacao, 2);
                amortizacaoDia = Math.Round(prestacaoDia - jurosPeriodoDia, 2);

                saldoDevedor = Math.Round(saldoDevedor - amortizacaoDia, 2);

                if (dia == prazoDias || saldoDevedor < 0)
                {
                    saldoDevedor = 0m;
                }

                jurosAcumuladosPeriodo = 0m;
            }

            response.Evolucao.Add(new EvolucaoPriceDto
            {
                Dia = dia,
                Prestacao = prestacaoDia,
                JurosPeriodo = jurosPeriodoDia,
                Amortizacao = amortizacaoDia,
                SaldoAposPagar = saldoDevedor
            });
        }

        return response;
    }

    private decimal CalcularPrestacaoPrice(decimal principal, decimal taxaMensal, int prazoMeses)
    {
        if (taxaMensal == 0)
        {
            return Math.Round(principal / prazoMeses, 2);
        }

        double p = (double)principal;
        double i = (double)taxaMensal;
        int n = prazoMeses;

        double prestacao = p * (i * Math.Pow(1 + i, n)) / (Math.Pow(1 + i, n) - 1);

        return Math.Round((decimal)prestacao, 2);
    }
}