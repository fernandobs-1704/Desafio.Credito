namespace Desafio.Credito.Shared.Dtos.Price;

public class CalcularPriceRequestDto
{
    public decimal ValorEmprestimo { get; set; }
    public decimal TaxaJurosMensal { get; set; }
    public int PrazoMeses { get; set; }
}