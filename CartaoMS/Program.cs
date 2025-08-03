using CartaoMS.Aplicacao.Servicos;
using CartaoMS.Infraestrutura.Contexto;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configura banco de dados
        services.AddDbContext<SqlContexto>(options =>
            options.UseSqlite(hostContext.Configuration.GetConnectionString("localDb")));

        services.AddRabbitMqApp(hostContext.Configuration);

    })
    .Build()
    .Run();
