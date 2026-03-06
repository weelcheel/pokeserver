using Microsoft.EntityFrameworkCore;
using PokeEntities;
using PokeEntities.Postgres;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;
        var postgresConnString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") ??
                                 (config.GetConnectionString("Postgres") ?? "");

        services.AddHostedService<Worker>()
            .AddDbContext<AppDbContext>(options => options.UseNpgsql(postgresConnString));
    })
    .Build();

host.Run();
