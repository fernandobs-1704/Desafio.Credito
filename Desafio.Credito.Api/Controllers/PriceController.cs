using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Credito.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceController : ControllerBase
{
    private readonly ICalculadoraPriceService _calculadoraPriceService;

    public PriceController(ICalculadoraPriceService calculadoraPriceService)
    {
        _calculadoraPriceService = calculadoraPriceService;
    }

    [HttpPost("calcular")]
    [ProducesResponseType(typeof(CalcularPriceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Calcular([FromBody] CalcularPriceRequestDto request)
    {
        if (request == null)
        {
            return BadRequest("Payload não informado.");
        }

        if (request.ValorEmprestimo <= 0)
        {
            return BadRequest("O valor do empréstimo deve ser maior que zero.");
        }

        if (request.TaxaJurosMensal <= 0)
        {
            return BadRequest("A taxa de juros mensal deve ser maior que zero.");
        }

        if (request.PrazoMeses <= 0)
        {
            return BadRequest("O prazo em meses deve ser maior que zero.");
        }

        var response = _calculadoraPriceService.Calcular(request);

        return Ok(response);
    }
}