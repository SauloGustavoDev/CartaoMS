using Shared.Enum;
namespace Shared.Modelos
{
    public class Cartao
    {
        public Guid Id { get; set; }
        public decimal Limite { get; set; }
        public string Numero { get; set; }
        public string Cvv { get; set; }
        public StatusCartao Status { get; set; }
    }
}
