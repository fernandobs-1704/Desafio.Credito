namespace Desafio.Credito.Infrastructure.Data.Entities;

public class EvolucaoContrato
{
    public long Id { get; set; }
    public int Dia { get; set; }
    public decimal Prestacao { get; set; }
    public decimal JurosPeriodo { get; set; }
    public decimal Amortizacao { get; set; }
    public decimal SaldoAposPagar { get; set; }
    public DateTime DataProcessamento { get; set; }
}