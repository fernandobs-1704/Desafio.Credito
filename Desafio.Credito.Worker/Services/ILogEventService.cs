using Desafio.Credito.Worker;
using Desafio.Credito.Worker.Logging;

namespace Desafio.Credito.Worker.Services;

public interface ILogEventService
{
    Task LogAsync(LogDto log);
}