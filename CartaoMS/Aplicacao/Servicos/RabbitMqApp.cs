using MassTransit;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace CartaoMS.Aplicacao.Servicos
{
    public static class RabbitMqApp
    {
        public static void AddRabbitMqApp(this IServiceCollection services, IConfiguration _config)
        {
            var retryPolicy = Policy
                            .Handle<Exception>()
                            .WaitAndRetry(3, retry => TimeSpan.FromSeconds(2));

            retryPolicy.Execute(() => TestaRabbitConnection(_config));


            services.AddMassTransit(busConfigurator =>
            {
                busConfigurator.AddConsumer<ClienteCriadoCartaoConsumidor>();
                busConfigurator.AddConsumer<CriarCartaoConsumidor>();

                busConfigurator.UsingRabbitMq((ctx, cfg) => {
                    cfg.Host(_config["RabbitConnection:host"], host =>
                    {
                        host.Username(_config["RabbitConnection:username"]);
                        host.Password(_config["RabbitConnection:password"]);
                    });

                    cfg.ReceiveEndpoint("cliente-criado-cartao", e =>
                    {
                        e.ConfigureConsumer<ClienteCriadoCartaoConsumidor>(ctx);
                    });

                    cfg.ReceiveEndpoint("gerar-cartao-cliente", e =>
                    {
                        e.ConfigureConsumer<CriarCartaoConsumidor>(ctx);
                    });

                });
            });
        }

        private static void TestaRabbitConnection(IConfiguration config)
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(config["RabbitConnection:host"]),
                UserName = config["RabbitConnection:username"],
                Password = config["RabbitConnection:password"]
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
        }
    }
}
