
using Shared.Enum;

namespace Shared.Modelos
{
    public class Proposta
    {
        public Guid Id { get; set; }
        public decimal ValorOfertado { get; set; }
        public StatusProposta Status {get; set;}
    }
}
