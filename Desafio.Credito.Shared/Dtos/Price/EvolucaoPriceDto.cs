namespace Desafio.Credito.Shared.Dtos.Price;

public class EvolucaoPriceDto
{
    public int Dia { get; set; }
    public decimal Prestacao { get; set; }
    public decimal JurosPeriodo { get; set; }
    public decimal Amortizacao { get; set; }
    public decimal SaldoAposPagar { get; set; }
}