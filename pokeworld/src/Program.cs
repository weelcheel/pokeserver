using PokeFramework.Redis;
using PokeWorld;
using PokeWorld.Processors;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<RedisClient>(provider =>
    new RedisClient("redis", provider.GetService<ILogger<RedisClient>>()));
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<GameWorld>();
builder.Services.AddSingleton<GameWorldProcessor>();

var host = builder.Build();

host.Services.GetRequiredService<GameWorldProcessor>();

host.Run();