using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace PokeFramework.Redis;

public class RedisClient(string connectionString, ILogger<RedisClient>? logger)
{
    private readonly ConnectionMultiplexer _connection = ConnectionMultiplexer.Connect(connectionString);

    public async Task PublishMessageAsync(string channel, RedisValue message)
    {
        var pubSub = _connection.GetSubscriber();

        await pubSub.PublishAsync(RedisChannel.Literal(channel), message);
    }

    public async Task SubscribeToChannelAsync(string channel, Action<RedisChannel, RedisValue> handler)
    {
        var pubSub = _connection.GetSubscriber();

        await pubSub.SubscribeAsync(RedisChannel.Literal(channel), handler);
        logger?.LogInformation("Subscribed to channel {channel}", channel);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var db = _connection.GetDatabase();
        var serializedValue = JsonSerializer.Serialize(value);
        return await db.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task<bool> SetRawAsync(string key, string value, TimeSpan? expiry = null)
    {
        var db = _connection.GetDatabase();
        var serializedValue = value;
        return await db.StringSetAsync(key, serializedValue, expiry);
    }
    
    public bool Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var db = _connection.GetDatabase();
        return db.StringSet(key, JsonSerializer.Serialize(value), expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = _connection.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task<string?> GetRawAsync(string key)
    {
        var db = _connection.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }
    
    public T? Get<T>(string key)
    {
        var db = _connection.GetDatabase();
        var value = db.StringGet(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var db = _connection.GetDatabase();
        return await db.KeyDeleteAsync(key);
    }
}