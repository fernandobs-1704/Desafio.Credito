namespace Desafio.Credito.Worker.Logging
{
    public class LogDto
    {
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string QueueName { get; set; }
        public string Payload { get; set; }
        public string Detalhe { get; set; }
        public string Exception { get; set; }
        public string MachineName { get; set; }
        public string Ambiente { get; set; }
        public DateTime DataHoraUtc { get; set; }
        public LogAcao Acao { get; set; }
        public LogStatus Status { get; set; }
    }
}
