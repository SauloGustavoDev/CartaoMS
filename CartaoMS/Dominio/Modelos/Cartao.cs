using Shared.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
