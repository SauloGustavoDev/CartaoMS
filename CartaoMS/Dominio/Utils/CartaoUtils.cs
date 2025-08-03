using Shared.Enum;
using Shared.Modelos;
namespace CartaoMS.Dominio.Utils
{
    public static class CartaoUtils
    {
        public static Cartao GerarCartao(decimal renda)
        {
            var chars = "0123456789";
            var numberString = new char[12];
            var numberString = new char[12];
            var cvvString = new char[3];
            var random = new Random();

            for (int i = 0; i < numberString.Length; i++)
            {
                numberString[i] = chars[random.Next(chars.Length)];
            }

            for (int i = 0; i < cvvString.Length; i++)
            {
                cvvString[i] = chars[random.Next(chars.Length)];
            }

            return new Cartao
            {
                Id = Guid.NewGuid(),
                Numero = new string(numberString),
                Cvv = new string(cvvString),
                Limite = renda * 1.5m,
                Status = StatusCartao.Ativo
            };
        }
        public static Erro SalvaErro(Guid id, string tipo, string mensagem, string trace)
        {
            return new Erro
            {
                Id = Guid.NewGuid(),
                ClienteId = id,
                Tipo = tipo,
                DtErro = DateTime.Now,
                Mensagem = mensagem,
                StackTrace = trace
            };
        }
    }
}
