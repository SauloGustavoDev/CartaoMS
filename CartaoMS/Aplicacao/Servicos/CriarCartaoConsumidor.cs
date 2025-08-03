using CartaoMS.Dominio.Utils;
using CartaoMS.Infraestrutura.Contexto;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Rabbit.Dominio.Eventos;
using Shared.Enum;
using Shared.Modelos;

namespace CartaoMS.Aplicacao.Servicos
{
    public class CriarCartaoConsumidor : IConsumer<CriarCartaoEvento>
    {
        private readonly SqlContexto _sqlContexto;
        private readonly IPublishEndpoint _bus;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger<CriarCartaoConsumidor> _logger;

        public CriarCartaoConsumidor(SqlContexto contexto, IPublishEndpoint bus, ILogger<CriarCartaoConsumidor> logger)
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
        public async Task Consume(ConsumeContext<CriarCartaoEvento> context)
        {
            Cliente cliente;
            try
            {

                cliente = _sqlContexto.Set<Cliente>()
                .AsNoTracking()
                .Include(x => x.Cartao)
                .FirstOrDefault(x => x.Id == context.Message.IdCliente);

                if (cliente == null)
                    throw new Exception($"Cliente com Id {context.Message.IdCliente} não encontrado.");



                var cartao = CartaoUtils.GerarCartao(cliente.Renda);
                if (cliente.Cartao == null || cliente.Cartao.Count == 0)
                    cliente.Cartao = new List<Cartao>();
                cliente.Cartao.Add(cartao);
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _sqlContexto.AddAsync(cartao);
                    _sqlContexto.Update(cliente);
                    await _sqlContexto.SaveChangesAsync();
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar CriarCartaoEvento");

                await _sqlContexto.AddAsync(CartaoUtils.SalvaErro(context.Message.IdCliente, context.Message.GetType().Name, ex.Message, ex.StackTrace));
                await _retryPolicy.ExecuteAsync(() => _sqlContexto.SaveChangesAsync());
            }

        }
    }
}
