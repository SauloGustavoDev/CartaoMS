namespace Shared.Modelos
{
    public class Erro
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public string Tipo { get; set; }
        public DateTime DtErro { get; set; } = DateTime.Now;
        public string Mensagem { get; set; }
        public string StackTrace { get; set; }
    }
}
