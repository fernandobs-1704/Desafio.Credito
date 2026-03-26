using System.Net.Http.Json;
using Desafio.Credito.Shared.Dtos.Price;

namespace Desafio.Credito.Worker.Services;

public class PriceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PriceApiClient> _logger;

    public PriceApiClient(HttpClient httpClient, ILogger<PriceApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CalcularPriceResponseDto?> CalcularAsync(
        CalcularPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Chamando API de cálculo PRICE...");

        var response = await _httpClient.PostAsJsonAsync(
            "api/price/calcular",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError("Erro ao chamar API. Status: {StatusCode}. Corpo: {Erro}",
                response.StatusCode, erro);

            return null;
        }

        var resultado = await response.Content.ReadFromJsonAsync<CalcularPriceResponseDto>(cancellationToken: cancellationToken);

        return resultado;
    }
}