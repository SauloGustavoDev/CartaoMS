namespace CartaoMS.Dominio.Eventos
{
    public class ClienteCriadoEvento
    {
        public Guid Id { get; set; }
        public bool SimularErro { get; set; }
    }
}
