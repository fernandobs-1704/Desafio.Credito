using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Domain.Interfaces;

public interface ICalculadoraPriceService
{
    CalcularPriceResponseDto Calcular(CalcularPriceRequestDto request);
}