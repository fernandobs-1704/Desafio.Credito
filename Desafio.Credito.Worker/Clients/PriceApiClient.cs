using Desafio.Credito.Shared.Dtos.Price;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desafio.Credito.Worker.Clients;

public class PriceApiClient
{
    private readonly HttpClient _httpClient;

    public PriceApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CalcularPriceResponseDto> CalcularAsync(CalcularPriceRequestDto request)
    {
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/price/calcular", content);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<CalcularPriceResponseDto>(responseJson);
    }
}