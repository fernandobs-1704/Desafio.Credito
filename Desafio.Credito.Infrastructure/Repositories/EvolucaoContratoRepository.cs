using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Infrastructure.Data;
using Desafio.Credito.Infrastructure.Data.Entities;
using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Infrastructure.Repositories;

public class EvolucaoContratoRepository : IEvolucaoContratoRepository
{
    private readonly AppDbContext _context;

    public EvolucaoContratoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SalvarAsync(CalcularPriceResponseDto response, CancellationToken cancellationToken = default)
    {
        if (response == null || response.Evolucao == null || response.Evolucao.Count == 0)
        {
            return;
        }

        //Repetir a mesma data para o processamento da mensagem
        var dataProcessamento = DateTime.UtcNow;

        var entidades = response.Evolucao.Select(item => new EvolucaoContrato
        {
            Dia = item.Dia,
            Prestacao = item.Prestacao,
            JurosPeriodo = item.JurosPeriodo,
            Amortizacao = item.Amortizacao,
            SaldoAposPagar = item.SaldoAposPagar,
            DataProcessamento = dataProcessamento
        }).ToList();

        await _context.EvolucoesContrato.AddRangeAsync(entidades, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}