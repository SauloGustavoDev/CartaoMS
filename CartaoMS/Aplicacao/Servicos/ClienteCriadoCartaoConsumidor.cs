using CartaoMS.Dominio.Utils;
using CartaoMS.Infraestrutura.Contexto;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Rabbit.Dominio.Eventos;
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



                var proposta = CartaoUtils.GerarCartao(cliente.Renda);
                if (cliente.Cartao == null)
                    cliente.Cartao = new List<Cartao>();
                cliente.Cartao.Add(proposta);
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _sqlContexto.AddAsync(proposta);
                    _sqlContexto.Update(cliente);
                    await _sqlContexto.SaveChangesAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar ClienteCriadoEvento");

                await _sqlContexto.AddAsync(CartaoUtils.SalvaErro(context.Message.Id, context.Message.GetType().Name, ex.Message, ex.StackTrace));
                await _retryPolicy.ExecuteAsync(() => _sqlContexto.SaveChangesAsync());

                await _retryPolicy.ExecuteAsync(() =>
                       _bus.Publish(new CartaoFalhaEvento
                       {
                           IdCliente = context.Message.Id,
                       })
                   );

            }

        }
    }
}
