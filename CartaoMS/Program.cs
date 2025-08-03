using CartaoMS.Aplicacao.Servicos;
using CartaoMS.Infraestrutura.Contexto;
using Microsoft.EntityFrameworkCore;
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
