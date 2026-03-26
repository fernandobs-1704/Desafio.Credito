using Desafio.Credito.Domain.Interfaces;
using Desafio.Credito.Shared.Dtos.Price;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Credito.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceController : ControllerBase
{
    private readonly ICalculadoraPriceService _calculadoraPriceService;
    private readonly IEvolucaoContratoRepository _evolucaoContratoRepository;

    public PriceController(
        ICalculadoraPriceService calculadoraPriceService,
        IEvolucaoContratoRepository evolucaoContratoRepository)
    {
        _calculadoraPriceService = calculadoraPriceService;
        _evolucaoContratoRepository = evolucaoContratoRepository;
    }

    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular(
        [FromBody] CalcularPriceRequestDto request,
        CancellationToken cancellationToken)
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

        await _evolucaoContratoRepository.SalvarAsync(response, cancellationToken);

        return Ok(response);
    }
}