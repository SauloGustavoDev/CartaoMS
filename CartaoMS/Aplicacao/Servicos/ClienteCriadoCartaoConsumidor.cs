using CartaoMS.Dominio.Eventos;
using CartaoMS.Infraestrutura.Contexto;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Shared.Enum;
using Shared.Modelos;

namespace CartaoMS.Aplicacao.Servicos
{
    public class ClienteCriadoCartaoConsumidor : IConsumer<ClienteCriadoEvento>
    {
        private readonly SqlContexto _sqlContexto;
        private readonly IPublishEndpoint _bus;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger<ClienteCriadoCartaoConsumidor> _logger;

        public ClienteCriadoCartaoConsumidor(SqlContexto contexto, IPublishEndpoint bus, ILogger<ClienteCriadoCartaoConsumidor> logger)
        {
            _sqlContexto = contexto;
            _logger = logger;
            _bus = bus;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(2),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, $"Erro ao publicar evento (tentativa {retryCount}). Tentando novamente em {timeSpan.TotalSeconds} segundos.");
                    });
        }

        public async Task Consume(ConsumeContext<ClienteCriadoEvento> context)
        {
            Cliente cliente;
            try
            {
                if (context.Message.SimularErro)
                {
                    throw new Exception($"Erro ao gerar o cartão do cliente de Id:{context.Message.Id}");
                }

                cliente = _sqlContexto.Set<Cliente>()
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == context.Message.Id);

                if (cliente == null)
                    throw new Exception($"Cliente com Id {context.Message.Id} não encontrado.");

               

                var proposta = GerarCartao(cliente.Renda);
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _sqlContexto.AddAsync(proposta);
                    await _sqlContexto.SaveChangesAsync();
                });

                await _retryPolicy.ExecuteAsync(() => _bus.Publish(new CartaoCriadoEvento { Id = proposta.Id }));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar ClienteCriadoEvento");

                await _sqlContexto.AddAsync(SalvaErro(context.Message.Id, context.Message.GetType().Name, ex.Message, ex.StackTrace));
                await _retryPolicy.ExecuteAsync(() => _sqlContexto.SaveChangesAsync());

                await _retryPolicy.ExecuteAsync(() =>
                       _bus.Publish(new CartaoFalhaEvento
                       {
                           ClienteId = context.Message.Id,
                           Motivo = ex.Message,
                           DataOcorrencia = DateTime.Now
                       })
                   );

            }

        }
        private Cartao GerarCartao(decimal renda)
        {
            var chars = "0123456789";
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

        private Erro SalvaErro(Guid id, string tipo, string mensagem, string trace)
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
