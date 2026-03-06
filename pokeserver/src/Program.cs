using PokeFramework.Redis;
using PokeServer;
using PokeServer.Game.Server;
using PokeServer.Handlers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<RedisClient>(provider =>
    new RedisClient("redis", provider.GetService<ILogger<RedisClient>>()));
builder.Services.AddSingleton<GameServer>();

// Add AppDbContext for user authentication
var postgresConnString = builder.Configuration.GetConnectionString("Postgres") ??
    "Host=localhost;Database=pokemmo;Username=postgres;Password=devpassword";
builder.Services.AddDbContext<PokeEntities.Postgres.AppDbContext>(options =>
    options.UseNpgsql(postgresConnString));

builder.Services.AddScoped<UnauthenticatedHandlers>();

var host = builder.Build();

// initialize processors
using var scope = host.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<UnauthenticatedHandlers>();

host.Run();