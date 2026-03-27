namespace Desafio.Credito.Worker.Logging;

public enum LogStatus
{
    Iniciado = 1,
    Sucesso = 2,
    ErroValidacao = 3,
    ErroApi = 4,
    ErroPersistencia = 5,
    ErroEventHub = 6,
    ErroServiceBus = 7,
    DeadLetter = 8,
    Abandonado = 9
}
