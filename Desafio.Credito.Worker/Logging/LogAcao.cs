namespace Desafio.Credito.Worker.Logging;

public enum LogAcao
{
    InicioProcessamento = 1,
    ValidacaoMensagem = 2,
    ChamadaApiPrice = 3,
    PersistenciaBanco = 4,
    PublicacaoEventHub = 5,
    FimProcessamento = 6,
    DeadLetter = 7,
    AbandonoMensagem = 8,
    ErroInfraestrutura = 9
}
