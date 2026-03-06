using Microsoft.EntityFrameworkCore;
using PokeFramework.Redis;
using PokeWorld;
using PokeWorld.Processors;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<RedisClient>(provider =>
    new RedisClient("redis", provider.GetService<ILogger<RedisClient>>()));
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<GameWorld>();
builder.Services.AddScoped<GameWorldProcessor>();

// Add AppDbContext for user authentication
var postgresConnString = builder.Configuration.GetConnectionString("Postgres") ??
                         "Host=localhost;Database=pokemmo;Username=postgres;Password=devpassword";
builder.Services.AddDbContext<PokeEntities.Postgres.AppDbContext>(options =>
    options.UseNpgsql(postgresConnString));

var host = builder.Build();

using var scope = host.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<GameWorldProcessor>();

host.Run();