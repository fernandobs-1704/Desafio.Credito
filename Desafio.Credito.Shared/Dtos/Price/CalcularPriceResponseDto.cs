using System.Collections.Generic;

namespace Desafio.Credito.Shared.Dtos.Price;

public class CalcularPriceResponseDto
{
    public List<EvolucaoPriceDto> Evolucao { get; set; }

    public CalcularPriceResponseDto()
    {
        Evolucao = new List<EvolucaoPriceDto>();
    }
}