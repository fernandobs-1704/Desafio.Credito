using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Worker.Clients;

public interface IPriceApiClient
{
    Task<CalcularPriceResponseDto> CalcularAsync(CalcularPriceRequestDto request);
}