namespace CartaoMS.Dominio.Eventos
{
    public class CartaoFalhaEvento
    {
        public Guid ClienteId { get; set; }       // Cliente que originou a tentativa
        public string Motivo { get; set; }        // Descrição da falha
        public DateTime DataOcorrencia { get; set; } = DateTime.UtcNow;
    }
}
