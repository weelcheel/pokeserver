using System.Text.Json;

namespace PokeFramework.Redis;

public static class RedisHelper
{
    public static async Task SendMessageToConnectionAsync(RedisClient redisClient, string connectionId,
        object message)
    {
        await redisClient.PublishMessageAsync($"connection-{connectionId}", JsonSerializer.Serialize(message));
    }
}