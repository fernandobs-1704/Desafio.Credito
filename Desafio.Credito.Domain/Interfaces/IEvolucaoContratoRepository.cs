using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Domain.Interfaces;

public interface IEvolucaoContratoRepository
{
    Task SalvarAsync(CalcularPriceResponseDto response, CancellationToken cancellationToken = default);
}