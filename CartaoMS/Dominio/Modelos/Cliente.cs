using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Modelos
{
    public class Cliente
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public decimal Renda { get; set; }
        public List<Proposta> Proposta { get; set; }
        public List<Cartao> Cartao { get; set; }
    }
}
