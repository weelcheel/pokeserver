using PokeFramework.Redis;
using PokeServer;
using PokeServer.Game.Server;
using PokeServer.Handlers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<RedisClient>(provider =>
    new RedisClient("redis", provider.GetService<ILogger<RedisClient>>()));
builder.Services.AddSingleton<GameServer>();
builder.Services.AddSingleton<UnauthenticatedHandlers>();

var host = builder.Build();

// initialize processors
host.Services.GetRequiredService<UnauthenticatedHandlers>();

host.Run();